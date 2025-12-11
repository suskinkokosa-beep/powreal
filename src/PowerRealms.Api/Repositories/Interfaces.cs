public interface IWithdrawalRepository
{
    Task<WithdrawalRequest> AddAsync(WithdrawalRequest w, CancellationToken ct = default);
    Task<WithdrawalRequest?> GetAsync(Guid id, CancellationToken ct = default);
    Task UpdateAsync(WithdrawalRequest w, CancellationToken ct = default);
    Task<IEnumerable<WithdrawalRequest>> GetByPoolAsync(Guid poolId, CancellationToken ct = default);
}
