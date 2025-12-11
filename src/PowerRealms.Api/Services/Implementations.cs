using PowerRealms.Api.Models;
using PowerRealms.Api.Repositories;
using PowerRealms.Api.Data;

namespace PowerRealms.Api.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IConfiguration _config;
    public AuthService(IUserRepository users, IConfiguration config) { _users = users; _config = config; }
    public async Task<User> RegisterAsync(string username, string password, UserRole role = UserRole.Member, CancellationToken ct = default)
    {
        var existing = await _users.GetByUsernameAsync(username, ct);
        if (existing != null) throw new InvalidOperationException("User already exists");
        var hash = BCrypt.Net.BCrypt.HashPassword(password);
        var user = new User { Username = username, PasswordHash = hash, Role = role, IsGlobalAdmin = role == UserRole.GlobalAdmin };
        return await _users.AddAsync(user, ct);
    }
    public async Task<string?> AuthenticateAsync(string username, string password, CancellationToken ct = default)
    {
        var user = await _users.GetByUsernameAsync(username, ct);
        if (user == null) return null;
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)) return null;
        var jwtSection = _config.GetSection("Jwt");
        var key = jwtSection.GetValue<string>("Key") ?? throw new InvalidOperationException("JWT key missing");
        var issuer = jwtSection.GetValue<string>("Issuer");
        var audience = jwtSection.GetValue<string>("Audience");
        var expiryMinutes = jwtSection.GetValue<int>("ExpireMinutes");
        var claims = new[] { new System.Security.Claims.Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, user.Id.ToString()), new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, user.Role.ToString()), new System.Security.Claims.Claim("id", user.Id.ToString()) };
        var keyBytes = System.Text.Encoding.UTF8.GetBytes(key);
        var securityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(keyBytes);
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(securityKey, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(issuer: issuer, audience: audience, claims: claims, expires: DateTime.UtcNow.AddMinutes(expiryMinutes), signingCredentials: creds);
        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }
}

public class LedgerService : ILedgerService
{
    private readonly ILedgerRepository _repo;
    public LedgerService(ILedgerRepository repo) => _repo = repo;
    public Task<PoolLedgerEntry> RecordTransactionAsync(PoolLedgerEntry entry, CancellationToken ct = default) => _repo.AddAsync(entry, ct);
    public Task<IEnumerable<PoolLedgerEntry>> GetPoolLedgerAsync(Guid poolId, CancellationToken ct = default) => _repo.GetByPoolAsync(poolId, ct);
}

public class HoldService : IHoldService
{
    private readonly IHoldRepository _repo;
    private readonly ILedgerService _ledger;
    public HoldService(IHoldRepository repo, ILedgerService ledger) { _repo = repo; _ledger = ledger; }
    public async Task<Hold> CreateHoldAsync(Hold h, CancellationToken ct = default) { var created = await _repo.AddAsync(h, ct); await _ledger.RecordTransactionAsync(new PoolLedgerEntry { PoolId = h.PoolId, FromUserId = h.FromUserId, ToUserId = Guid.Empty, Amount = h.Amount, Memo = "Hold Created", Status = TransactionStatus.Pending }, ct); return created; }
    public async Task<Hold?> ReleaseHoldAsync(Guid holdId, bool success, CancellationToken ct = default) { var hold = await _repo.GetAsync(holdId, ct); if (hold == null) return null; hold.Status = success ? TransactionStatus.Released : TransactionStatus.Cancelled; await _repo.UpdateAsync(hold, ct); return hold; }
}

public class PoolService : IPoolService
{
    private readonly IPoolRepository _repo;
    public PoolService(IPoolRepository repo) => _repo = repo;
    public Task<Pool> CreatePoolAsync(Pool pool, CancellationToken ct = default) => _repo.CreateAsync(pool, ct);
    public Task<Pool?> GetPoolAsync(Guid poolId, CancellationToken ct = default) => _repo.GetAsync(poolId, ct);
    public Task<IEnumerable<Pool>> GetAllPoolsAsync(CancellationToken ct = default) => _repo.ListAsync(ct);
}

public class MarketplaceService : IMarketplaceService
{
    private readonly IOfferRepository _repo;
    public MarketplaceService(IOfferRepository repo) => _repo = repo;
    public Task<Offer> CreateOfferAsync(Offer offer, CancellationToken ct = default) => _repo.AddAsync(offer, ct);
    public Task<IEnumerable<Offer>> GetOffersAsync(Guid poolId, CancellationToken ct = default) => _repo.GetByPoolAsync(poolId, ct);
}

public class GameBoostService : IGameBoostService
{
    private readonly IGameSessionRepository _sessions;
    private readonly IHoldService _holdService;
    private readonly ILedgerService _ledger;
    private readonly IPoolRepository _poolRepo;
    private readonly PowerRealmsDbContext _db;
    public GameBoostService(IGameSessionRepository sessions, IHoldService holdService, ILedgerService ledger, IPoolRepository poolRepo, PowerRealmsDbContext db) { _sessions = sessions; _holdService = holdService; _ledger = ledger; _poolRepo = poolRepo; _db = db; }
    public async Task<GameSession> StartSessionAsync(Guid poolId, Guid nodeId, Guid seekerId, int minutes, CancellationToken ct = default)
    {
        var session = new GameSession { PoolId = poolId, NodeId = nodeId, SeekerId = seekerId, MinutesReserved = minutes, PricePerMinute = 0.5m, TotalHeld = minutes * 0.5m, Status = GameSessionStatus.Active, StartedAt = DateTime.UtcNow };
        var hold = new Hold { PoolId = poolId, FromUserId = seekerId, Amount = session.TotalHeld, Type = HoldType.Payment, Status = TransactionStatus.Pending };
        await _holdService.CreateHoldAsync(hold, ct);
        await _sessions.AddAsync(session, ct);
        await _ledger.RecordTransactionAsync(new PoolLedgerEntry { PoolId = poolId, FromUserId = seekerId, ToUserId = nodeId, Amount = session.TotalHeld, Memo = "GameBoost Hold", Status = TransactionStatus.Pending }, ct);
        return session;
    }
    public async Task<bool> ReportMetricsAsync(Guid sessionId, GameMetric metric, CancellationToken ct = default) { var session = await _sessions.GetAsync(sessionId, ct); if (session == null) return false; if (session.Status != GameSessionStatus.Active) return false; metric.SessionId = sessionId; await _sessions.AddMetricAsync(metric, ct); return true; }
    public async Task<GameSession?> EndSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await _sessions.GetAsync(sessionId, ct); if (session == null) return null; if (session.Status != GameSessionStatus.Active) throw new InvalidOperationException("Session not active");
        session.EndedAt = DateTime.UtcNow; session.Status = GameSessionStatus.Ended;
        var metrics = (await _sessions.GetMetricsAsync(sessionId, ct)).ToList();
        if (metrics.Any()) { session.AvgLatencyMs = metrics.Average(m => m.LatencyMs); session.AvgFps = metrics.Average(m => m.Fps); session.UptimePercent = metrics.Average(m => m.UptimeFraction) * 100.0; } else { session.AvgLatencyMs = 9999; session.AvgFps = 0; session.UptimePercent = 0; }
        decimal refundPercent = 0.0m; if (session.AvgLatencyMs > 200) refundPercent += 0.3m; if (session.AvgFps < 30) refundPercent += 0.4m; if (session.UptimePercent < 90) refundPercent += 0.3m; if (refundPercent > 1m) refundPercent = 1m;
        var refundAmount = session.TotalHeld * refundPercent; var payout = session.TotalHeld - refundAmount; session.PayoutAmount = payout;
        if (refundAmount > 0) await _ledger.RecordTransactionAsync(new PoolLedgerEntry { PoolId = session.PoolId, FromUserId = session.SeekerId, ToUserId = Guid.Empty, Amount = -refundAmount, Memo = "GameBoost Partial Refund", Status = TransactionStatus.Completed }, ct);
        await _ledger.RecordTransactionAsync(new PoolLedgerEntry { PoolId = session.PoolId, FromUserId = session.SeekerId, ToUserId = session.NodeId, Amount = payout, Memo = "GameBoost Payout", Status = TransactionStatus.Completed }, ct);
        var node = await _db.Nodes.FindAsync(new object[]{ session.NodeId }, ct);
        if (node != null) { var score = 0.0; if (session.AvgLatencyMs <= 100) score += 0.5; if (session.AvgLatencyMs <= 50) score += 0.5; if (session.AvgFps >= 60) score += 1.0; if (session.UptimePercent >= 99) score += 1.0; node.Rating = Math.Max(1.0, Math.Min(5.0, node.Rating + (score - 1.0))); _db.Nodes.Update(node); await _db.SaveChangesAsync(ct); }
        await _sessions.UpdateAsync(session, ct); return session;
    }
    public Task<GameSession?> GetSessionAsync(Guid sessionId, CancellationToken ct = default) => _sessions.GetAsync(sessionId, ct);
}

public class PoolManagementService : IPoolManagementService
{
    private readonly IPoolRepository _poolRepo; private readonly IPoolMemberRepository _memberRepo;
    public PoolManagementService(IPoolRepository poolRepo, IPoolMemberRepository memberRepo) { _poolRepo = poolRepo; _memberRepo = memberRepo; }

    public async Task<bool> JoinPool(Guid poolId, Guid userId)
    {
        var pool = await _poolRepo.GetAsync(poolId); if (pool == null) return false;
        var existing = await _memberRepo.GetAsync(poolId, userId); if (existing != null) return true;
        var member = new PoolMember { PoolId = poolId, UserId = userId, Role = pool.OwnerId == userId ? PoolMemberRole.Owner : PoolMemberRole.Member, MachinesLimit = 10 };
        await _memberRepo.AddAsync(member); return true;
    }

    public async Task<bool> PromoteToOfficer(Guid poolId, Guid targetUser) { var member = await _memberRepo.GetAsync(poolId, targetUser); if (member == null) return false; member.Role = PoolMemberRole.Officer; await _memberRepo.UpdateAsync(member); return true; }

    public async Task<bool> AddReferral(Guid poolId, Guid userId) { var member = await _memberRepo.GetAsync(poolId, userId); if (member == null) return false; member.ReferralCount++; if (member.ReferralCount % 5 == 0) member.MachinesLimit += 1; await _memberRepo.UpdateAsync(member); return true; }

    public async Task<bool> AddMachine(Guid poolId, Guid userId) { var member = await _memberRepo.GetAsync(poolId, userId); if (member == null) return false; if (member.MachinesUsed >= member.MachinesLimit) return false; member.MachinesUsed++; await _memberRepo.UpdateAsync(member); return true; }
}
