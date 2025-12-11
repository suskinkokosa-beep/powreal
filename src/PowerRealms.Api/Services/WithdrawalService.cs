using PowerRealms.Api.Models;
using PowerRealms.Api.Repositories;

namespace PowerRealms.Api.Services;

public class WithdrawalService : IWithdrawalService
{
    private readonly IWithdrawalRepository _repo;
    private readonly IHoldService _holdService;
    private readonly ILedgerService _ledger;
    private readonly IPoolRepository _poolRepo;

    public WithdrawalService(IWithdrawalRepository repo, IHoldService holdService, ILedgerService ledger, IPoolRepository poolRepo)
    {
        _repo = repo;
        _holdService = holdService;
        _ledger = ledger;
        _poolRepo = poolRepo;
    }

    public async Task<WithdrawalRequest> CreateWithdrawalRequestAsync(Guid poolId, Guid userId, decimal amount, string externalAddress, CancellationToken ct = default)
    {
        // Create Hold to lock funds
        var hold = new Hold { PoolId = poolId, FromUserId = userId, Amount = amount, Type = HoldType.Withdrawal, Status = TransactionStatus.Pending };
        var createdHold = await _holdService.CreateHoldAsync(hold, ct);

        var req = new WithdrawalRequest
        {
            PoolId = poolId,
            UserId = userId,
            Amount = amount,
            ExternalAddress = externalAddress,
            Status = WithdrawalStatus.Requested,
            HoldId = createdHold.Id
        };

        await _repo.AddAsync(req, ct);
        await _ledger.RecordTransactionAsync(new PoolLedgerEntry { PoolId = poolId, FromUserId = userId, ToUserId = Guid.Empty, Amount = -amount, Memo = "Withdrawal Requested (Hold)", Status = TransactionStatus.Pending }, ct);
        return req;
    }

    public async Task<WithdrawalRequest?> ConfirmWithdrawalAsync(Guid withdrawalId, Guid confirmerId, bool approved, CancellationToken ct = default)
    {
        var req = await _repo.GetAsync(withdrawalId, ct);
        if (req == null) return null;
        if (req.Status != WithdrawalStatus.Requested) throw new InvalidOperationException("Withdrawal already processed");

        if (!approved)
        {
            if (req.HoldId.HasValue)
            {
                await _holdService.ReleaseHoldAsync(req.HoldId.Value, false, ct);
            }
            req.Status = WithdrawalStatus.Rejected;
            req.Memo = "Rejected by confirmer";
            req.ConfirmedAt = DateTime.UtcNow;
            await _repo.UpdateAsync(req, ct);
            await _ledger.RecordTransactionAsync(new PoolLedgerEntry { PoolId = req.PoolId, FromUserId = Guid.Empty, ToUserId = req.UserId, Amount = req.Amount, Memo = "Withdrawal Rejected - Refund", Status = TransactionStatus.Completed }, ct);
            return req;
        }

        // Approved: simulate external transfer (owner performs off-platform transfer); we mark it Completed
        if (req.HoldId.HasValue)
        {
            await _holdService.ReleaseHoldAsync(req.HoldId.Value, true, ct);
        }

        req.Status = WithdrawalStatus.Completed;
        req.ConfirmedAt = DateTime.UtcNow;
        req.CompletedAt = DateTime.UtcNow;
        req.Memo = "Approved and completed by confirmer";
        await _repo.UpdateAsync(req, ct);

        await _ledger.RecordTransactionAsync(new PoolLedgerEntry { PoolId = req.PoolId, FromUserId = req.UserId, ToUserId = Guid.Empty, Amount = -req.Amount, Memo = "Withdrawal Completed - Off-platform", Status = TransactionStatus.Completed }, ct);
        return req;
    }

    public Task<IEnumerable<WithdrawalRequest>> GetPoolWithdrawalsAsync(Guid poolId, CancellationToken ct = default) => _repo.GetByPoolAsync(poolId, ct);
    public Task<WithdrawalRequest?> GetAsync(Guid id, CancellationToken ct = default) => _repo.GetAsync(id, ct);
}
