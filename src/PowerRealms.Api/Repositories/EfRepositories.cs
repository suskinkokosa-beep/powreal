using Microsoft.EntityFrameworkCore;
using PowerRealms.Api.Data;
using PowerRealms.Api.Models;

namespace PowerRealms.Api.Repositories;

public class UserRepository : IUserRepository
{
    private readonly PowerRealmsDbContext _db;
    public UserRepository(PowerRealmsDbContext db) => _db = db;
    public Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default) => _db.Users.FirstOrDefaultAsync(u => u.Username == username, ct);
    public async Task<User> AddAsync(User user, CancellationToken ct = default) { _db.Users.Add(user); await _db.SaveChangesAsync(ct); return user; }
}

public class PoolRepository : IPoolRepository
{
    private readonly PowerRealmsDbContext _db;
    public PoolRepository(PowerRealmsDbContext db) => _db = db;
    public async Task<Pool> CreateAsync(Pool pool, CancellationToken ct = default) { _db.Pools.Add(pool); await _db.SaveChangesAsync(ct); return pool; }
    public Task<Pool?> GetAsync(Guid id, CancellationToken ct = default) => _db.Pools.FindAsync(new object[]{id}, ct).AsTask();
    public Task<IEnumerable<Pool>> ListAsync(CancellationToken ct = default) => Task.FromResult(_db.Pools.AsEnumerable());
}

public class LedgerRepository : ILedgerRepository
{
    private readonly PowerRealmsDbContext _db;
    public LedgerRepository(PowerRealmsDbContext db) => _db = db;
    public async Task<PoolLedgerEntry> AddAsync(PoolLedgerEntry entry, CancellationToken ct = default) { _db.Ledger.Add(entry); await _db.SaveChangesAsync(ct); return entry; }
    public Task<IEnumerable<PoolLedgerEntry>> GetByPoolAsync(Guid poolId, CancellationToken ct = default) => Task.FromResult(_db.Ledger.Where(l => l.PoolId == poolId).AsEnumerable());
}

public class HoldRepository : IHoldRepository
{
    private readonly PowerRealmsDbContext _db;
    public HoldRepository(PowerRealmsDbContext db) => _db = db;
    public async Task<Hold> AddAsync(Hold hold, CancellationToken ct = default) { _db.Holds.Add(hold); await _db.SaveChangesAsync(ct); return hold; }
    public Task<Hold?> GetAsync(Guid id, CancellationToken ct = default) => _db.Holds.FindAsync(new object[]{id}, ct).AsTask();
    public async Task UpdateAsync(Hold hold, CancellationToken ct = default) { _db.Holds.Update(hold); await _db.SaveChangesAsync(ct); }
}

public class OfferRepository : IOfferRepository
{
    private readonly PowerRealmsDbContext _db;
    public OfferRepository(PowerRealmsDbContext db) => _db = db;
    public async Task<Offer> AddAsync(Offer offer, CancellationToken ct = default) { _db.Offers.Add(offer); await _db.SaveChangesAsync(ct); return offer; }
    public Task<IEnumerable<Offer>> GetByPoolAsync(Guid poolId, CancellationToken ct = default) => Task.FromResult(_db.Offers.Where(o => o.PoolId == poolId).AsEnumerable());
}

public class GameSessionRepository : IGameSessionRepository
{
    private readonly PowerRealmsDbContext _db;
    public GameSessionRepository(PowerRealmsDbContext db) => _db = db;
    public async Task<GameSession> AddAsync(GameSession s, CancellationToken ct = default) { _db.GameSessions.Add(s); await _db.SaveChangesAsync(ct); return s; }
    public Task<GameSession?> GetAsync(Guid id, CancellationToken ct = default) => _db.GameSessions.FindAsync(new object[]{id}, ct).AsTask();
    public async Task UpdateAsync(GameSession s, CancellationToken ct = default) { _db.GameSessions.Update(s); await _db.SaveChangesAsync(ct); }
    public async Task AddMetricAsync(GameMetric m, CancellationToken ct = default) { _db.GameMetrics.Add(m); await _db.SaveChangesAsync(ct); }
    public Task<IEnumerable<GameMetric>> GetMetricsAsync(Guid sessionId, CancellationToken ct = default) => Task.FromResult(_db.GameMetrics.Where(m => m.SessionId == sessionId).AsEnumerable());
}

public class PoolMemberRepository : IPoolMemberRepository
{
    private readonly PowerRealmsDbContext _db;
    public PoolMemberRepository(PowerRealmsDbContext db) => _db = db;
    public Task<PoolMember?> GetAsync(Guid poolId, Guid userId) => _db.PoolMembers.FirstOrDefaultAsync(x => x.PoolId == poolId && x.UserId == userId);
    public Task<List<PoolMember>> GetPoolMembersAsync(Guid poolId) => _db.PoolMembers.Where(x => x.PoolId == poolId).ToListAsync();
    public async Task AddAsync(PoolMember member) { _db.PoolMembers.Add(member); await _db.SaveChangesAsync(); }
    public async Task UpdateAsync(PoolMember member) { _db.PoolMembers.Update(member); await _db.SaveChangesAsync(); }
}

public class WithdrawalRepository : IWithdrawalRepository
{
    private readonly PowerRealmsDbContext _db;
    public WithdrawalRepository(PowerRealmsDbContext db) => _db = db;
    public async Task<WithdrawalRequest> AddAsync(WithdrawalRequest w, CancellationToken ct = default) { _db.WithdrawalRequests.Add(w); await _db.SaveChangesAsync(ct); return w; }
    public Task<WithdrawalRequest?> GetAsync(Guid id, CancellationToken ct = default) => _db.WithdrawalRequests.FindAsync(new object[]{id}, ct).AsTask();
    public async Task UpdateAsync(WithdrawalRequest w, CancellationToken ct = default) { _db.WithdrawalRequests.Update(w); await _db.SaveChangesAsync(ct); }
    public Task<IEnumerable<WithdrawalRequest>> GetByPoolAsync(Guid poolId, CancellationToken ct = default) => Task.FromResult(_db.WithdrawalRequests.Where(x => x.PoolId == poolId).AsEnumerable());
}
