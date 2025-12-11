using PowerRealms.Api.Models;

namespace PowerRealms.Api.Repositories;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<User> AddAsync(User user, CancellationToken ct = default);
}

public interface IPoolRepository
{
    Task<Pool> CreateAsync(Pool pool, CancellationToken ct = default);
    Task<Pool?> GetAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Pool>> ListAsync(CancellationToken ct = default);
}

public interface ILedgerRepository
{
    Task<PoolLedgerEntry> AddAsync(PoolLedgerEntry entry, CancellationToken ct = default);
    Task<IEnumerable<PoolLedgerEntry>> GetByPoolAsync(Guid poolId, CancellationToken ct = default);
}

public interface IHoldRepository
{
    Task<Hold> AddAsync(Hold hold, CancellationToken ct = default);
    Task<Hold?> GetAsync(Guid id, CancellationToken ct = default);
    Task UpdateAsync(Hold hold, CancellationToken ct = default);
}

public interface IOfferRepository
{
    Task<Offer> AddAsync(Offer offer, CancellationToken ct = default);
    Task<IEnumerable<Offer>> GetByPoolAsync(Guid poolId, CancellationToken ct = default);
}

public interface IGameSessionRepository
{
    Task<GameSession> AddAsync(GameSession session, CancellationToken ct = default);
    Task<GameSession?> GetAsync(Guid id, CancellationToken ct = default);
    Task UpdateAsync(GameSession session, CancellationToken ct = default);
    Task AddMetricAsync(GameMetric metric, CancellationToken ct = default);
    Task<IEnumerable<GameMetric>> GetMetricsAsync(Guid sessionId, CancellationToken ct = default);
}

public interface IPoolMemberRepository
{
    Task<PoolMember?> GetAsync(Guid poolId, Guid userId);
    Task<List<PoolMember>> GetPoolMembersAsync(Guid poolId);
    Task AddAsync(PoolMember member);
    Task UpdateAsync(PoolMember member);
}

public interface IWithdrawalRepository
{
    Task<WithdrawalRequest> AddAsync(WithdrawalRequest w, CancellationToken ct = default);
    Task<WithdrawalRequest?> GetAsync(Guid id, CancellationToken ct = default);
    Task UpdateAsync(WithdrawalRequest w, CancellationToken ct = default);
    Task<IEnumerable<WithdrawalRequest>> GetByPoolAsync(Guid poolId, CancellationToken ct = default);
}
