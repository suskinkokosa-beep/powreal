using Microsoft.EntityFrameworkCore;
using PowerRealms.Api.Models;

namespace PowerRealms.Api.Data;

public class PowerRealmsDbContext : DbContext
{
    public PowerRealmsDbContext(DbContextOptions<PowerRealmsDbContext> options) : base(options) { }
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Pool> Pools { get; set; } = null!;
    public DbSet<Node> Nodes { get; set; } = null!;
    public DbSet<PoolMember> PoolMembers { get; set; } = null!;
    public DbSet<PoolLedgerEntry> Ledger { get; set; } = null!;
    public DbSet<Hold> Holds { get; set; } = null!;
    public DbSet<MemberBalance> MemberBalances { get; set; } = null!;
    public DbSet<Offer> Offers { get; set; } = null!;
    public DbSet<GameSession> GameSessions { get; set; } = null!;
    public DbSet<GameMetric> GameMetrics { get; set; } = null!;
    public DbSet<WithdrawalRequest> WithdrawalRequests { get; set; } = null!;
    public DbSet<Dispute> Disputes { get; set; } = null!;
    public DbSet<BalanceTransaction> BalanceTransactions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<User>(b => { b.HasKey(u => u.Id); b.Property(u => u.Username).IsRequired(); });
        modelBuilder.Entity<Pool>(b => { b.HasKey(p => p.Id); b.Property(p => p.Name).IsRequired(); });
        modelBuilder.Entity<Node>(b => { b.HasKey(n => n.Id); });
        modelBuilder.Entity<PoolMember>(b => { b.HasKey(pm => pm.Id); });
        modelBuilder.Entity<PoolLedgerEntry>(b => { b.HasKey(l => l.Id); });
        modelBuilder.Entity<Hold>(b => { b.HasKey(h => h.Id); });
        modelBuilder.Entity<MemberBalance>(b => { b.HasKey(m => new { m.UserId, m.PoolId }); });
        modelBuilder.Entity<Offer>(b => { b.HasKey(o => o.Id); });
        modelBuilder.Entity<GameSession>(b => { b.HasKey(s => s.Id); });
        modelBuilder.Entity<GameMetric>(b => { b.HasKey(m => m.Id); });
        modelBuilder.Entity<WithdrawalRequest>(b => { b.HasKey(w => w.Id); });
        modelBuilder.Entity<Dispute>(b => { b.HasKey(d => d.Id); });
        modelBuilder.Entity<BalanceTransaction>(b => { b.HasKey(bt => bt.Id); });
    }
}
