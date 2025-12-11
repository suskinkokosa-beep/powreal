using PowerRealms.Api.Models;

namespace PowerRealms.Api.Services;

public interface IAuthService
{
    Task<User> RegisterAsync(string username, string password, UserRole role = UserRole.Member, CancellationToken ct = default);
    Task<string?> AuthenticateAsync(string username, string password, CancellationToken ct = default);
}

public interface ILedgerService
{
    Task<PoolLedgerEntry> RecordTransactionAsync(PoolLedgerEntry entry, CancellationToken ct = default);
    Task<IEnumerable<PoolLedgerEntry>> GetPoolLedgerAsync(Guid poolId, CancellationToken ct = default);
}

public interface IHoldService
{
    Task<Hold> CreateHoldAsync(Hold h, CancellationToken ct = default);
    Task<Hold?> ReleaseHoldAsync(Guid holdId, bool success, CancellationToken ct = default);
}

public interface IPoolService
{
    Task<Pool> CreatePoolAsync(Pool pool, CancellationToken ct = default);
    Task<Pool?> GetPoolAsync(Guid poolId, CancellationToken ct = default);
    Task<IEnumerable<Pool>> GetAllPoolsAsync(CancellationToken ct = default);
}

public interface IMarketplaceService
{
    Task<Offer> CreateOfferAsync(Offer offer, CancellationToken ct = default);
    Task<IEnumerable<Offer>> GetOffersAsync(Guid poolId, CancellationToken ct = default);
}

public interface IGameBoostService
{
    Task<GameSession> StartSessionAsync(Guid poolId, Guid nodeId, Guid seekerId, int minutes, CancellationToken ct = default);
    Task<bool> ReportMetricsAsync(Guid sessionId, GameMetric metric, CancellationToken ct = default);
    Task<GameSession?> EndSessionAsync(Guid sessionId, CancellationToken ct = default);
    Task<GameSession?> GetSessionAsync(Guid sessionId, CancellationToken ct = default);
}

public interface IPoolManagementService
{
    Task<bool> JoinPool(Guid poolId, Guid userId);
    Task<bool> PromoteToOfficer(Guid poolId, Guid targetUser);
    Task<bool> AddReferral(Guid poolId, Guid userId);
    Task<bool> AddMachine(Guid poolId, Guid userId);
}

public interface IWithdrawalService
{
    Task<WithdrawalRequest> CreateWithdrawalRequestAsync(Guid poolId, Guid userId, decimal amount, string externalAddress, CancellationToken ct = default);
    Task<WithdrawalRequest?> ConfirmWithdrawalAsync(Guid withdrawalId, Guid confirmerId, bool approved, CancellationToken ct = default);
    Task<IEnumerable<WithdrawalRequest>> GetPoolWithdrawalsAsync(Guid poolId, CancellationToken ct = default);
    Task<WithdrawalRequest?> GetAsync(Guid id, CancellationToken ct = default);
}
