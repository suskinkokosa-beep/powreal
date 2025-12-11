using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PowerRealms.Api.Data;
using PowerRealms.Api.P2P;
using PowerRealms.Api.Repositories;
using PowerRealms.Api.Services;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "PowerRealms API", Version = "v1" });
    var securityScheme = new OpenApiSecurityScheme {
        Name = "Authorization", Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT", In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    };
    c.AddSecurityDefinition("Bearer", securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { { securityScheme, new[] { "Bearer" } } });
});

var dbPath = configuration.GetValue<string>("Database:Path") ?? "powerrealms.db";
var connectionString = $"Data Source={dbPath}";
builder.Services.AddDbContext<PowerRealmsDbContext>(opt => opt.UseSqlite(connectionString));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPoolRepository, PoolRepository>();
builder.Services.AddScoped<ILedgerRepository, LedgerRepository>();
builder.Services.AddScoped<IHoldRepository, HoldRepository>();
builder.Services.AddScoped<IOfferRepository, OfferRepository>();
builder.Services.AddScoped<IGameSessionRepository, GameSessionRepository>();
builder.Services.AddScoped<IPoolMemberRepository, PoolMemberRepository>();
builder.Services.AddScoped<IWithdrawalRepository, WithdrawalRepository>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ILocalizationService, LocalizationService>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ILedgerService, LedgerService>();
builder.Services.AddScoped<IHoldService, HoldService>();
builder.Services.AddScoped<IPoolService, PoolService>();
builder.Services.AddScoped<IMarketplaceService, MarketplaceService>();
builder.Services.AddScoped<IGameBoostService, GameBoostService>();
builder.Services.AddScoped<IPoolManagementService, PoolManagementService>();
builder.Services.AddScoped<IWithdrawalService, WithdrawalService>();
builder.Services.AddScoped<IDisputeService, DisputeService>();
builder.Services.AddScoped<INodeService, NodeService>();
builder.Services.AddScoped<IBalanceService, BalanceService>();

var p2pNode = new P2PNode();
builder.Services.AddSingleton<IP2PNode>(p2pNode);
builder.Services.AddScoped<ISyncService, SyncService>();

var jwtSection = configuration.GetSection("Jwt");
var key = jwtSection.GetValue<string>("Key");
if (string.IsNullOrEmpty(key)) throw new InvalidOperationException("Jwt:Key is not configured");
var keyBytes = Encoding.UTF8.GetBytes(key);
builder.Services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options => {
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters {
        ValidateIssuer = true, ValidateAudience = true, ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSection.GetValue<string>("Issuer"), ValidAudience = jwtSection.GetValue<string>("Audience"),
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
    };
});
builder.Services.AddAuthorization();

builder.Services.Configure<ForwardedHeadersOptions>(options => {
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<PowerRealmsDbContext>();
    db.Database.EnsureCreated();

    var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
    var seed = configuration.GetSection("SeedAdmin");
    var username = seed.GetValue<string>("Username");
    var password = seed.GetValue<string>("Password");
    var isAdmin = seed.GetValue<bool>("IsGlobalAdmin");
    if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password)) {
        var existing = await userRepo.GetByUsernameAsync(username);
        if (existing == null) {
            var admin = new PowerRealms.Api.Models.User {
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = PowerRealms.Api.Models.UserRole.GlobalAdmin,
                IsGlobalAdmin = isAdmin
            };
            await userRepo.AddAsync(admin);
            Console.WriteLine($"Seeded admin user: {username}");
        }
    }
}

var p2pPort = configuration.GetValue<int>("P2P:Port", 5001);
await p2pNode.StartAsync(p2pPort);

var bootstrapPeers = configuration.GetSection("P2P:BootstrapPeers").Get<string[]>();
if (bootstrapPeers != null)
{
    foreach (var peer in bootstrapPeers)
    {
        _ = p2pNode.ConnectToPeerAsync(peer);
    }
}

app.Lifetime.ApplicationStopping.Register(() => p2pNode.StopAsync().Wait());

app.UseForwardedHeaders();
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run("http://0.0.0.0:5000");
