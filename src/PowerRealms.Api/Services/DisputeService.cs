using PowerRealms.Api.Data;
using PowerRealms.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace PowerRealms.Api.Services;

public interface IDisputeService
{
    Task<Dispute> CreateDisputeAsync(Dispute dispute, CancellationToken ct = default);
    Task<Dispute?> GetDisputeAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Dispute>> GetPoolDisputesAsync(Guid poolId, CancellationToken ct = default);
    Task<Dispute?> ResolveDisputeAsync(Guid disputeId, Guid resolverId, DisputeOutcome outcome, string resolution, decimal? refundAmount = null, CancellationToken ct = default);
}

public class DisputeService : IDisputeService
{
    private readonly PowerRealmsDbContext _db;
    private readonly ILedgerService _ledger;

    public DisputeService(PowerRealmsDbContext db, ILedgerService ledger)
    {
        _db = db;
        _ledger = ledger;
    }

    public async Task<Dispute> CreateDisputeAsync(Dispute dispute, CancellationToken ct = default)
    {
        _db.Disputes.Add(dispute);
        await _db.SaveChangesAsync(ct);
        return dispute;
    }

    public Task<Dispute?> GetDisputeAsync(Guid id, CancellationToken ct = default)
        => _db.Disputes.FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<IEnumerable<Dispute>> GetPoolDisputesAsync(Guid poolId, CancellationToken ct = default)
        => await _db.Disputes.Where(d => d.PoolId == poolId).ToListAsync(ct);

    public async Task<Dispute?> ResolveDisputeAsync(Guid disputeId, Guid resolverId, DisputeOutcome outcome, string resolution, decimal? refundAmount = null, CancellationToken ct = default)
    {
        var dispute = await _db.Disputes.FindAsync(new object[] { disputeId }, ct);
        if (dispute == null || dispute.Status == DisputeStatus.Resolved)
            return null;

        dispute.Status = DisputeStatus.Resolved;
        dispute.Outcome = outcome;
        dispute.ResolverId = resolverId;
        dispute.Resolution = resolution;
        dispute.RefundAmount = refundAmount;
        dispute.ResolvedAt = DateTime.UtcNow;

        if (refundAmount.HasValue && refundAmount > 0 && (outcome == DisputeOutcome.FullRefund || outcome == DisputeOutcome.PartialRefund))
        {
            await _ledger.RecordTransactionAsync(new PoolLedgerEntry
            {
                PoolId = dispute.PoolId,
                FromUserId = Guid.Empty,
                ToUserId = dispute.InitiatorId,
                Amount = refundAmount.Value,
                Memo = $"Dispute Resolution: {outcome}",
                Status = TransactionStatus.Completed
            }, ct);
        }

        _db.Disputes.Update(dispute);
        await _db.SaveChangesAsync(ct);
        return dispute;
    }
}
