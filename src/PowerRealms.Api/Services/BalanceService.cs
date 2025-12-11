using PowerRealms.Api.Data;
using PowerRealms.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace PowerRealms.Api.Services;

public interface IBalanceService
{
    Task<MemberBalance?> GetBalanceAsync(Guid userId, Guid poolId, CancellationToken ct = default);
    Task<MemberBalance> DepositAsync(Guid userId, Guid poolId, decimal amount, string description, CancellationToken ct = default);
    Task<bool> TransferAsync(Guid fromUserId, Guid toUserId, Guid poolId, decimal amount, string description, CancellationToken ct = default);
    Task<IEnumerable<BalanceTransaction>> GetTransactionHistoryAsync(Guid userId, Guid poolId, CancellationToken ct = default);
}

public class BalanceService : IBalanceService
{
    private readonly PowerRealmsDbContext _db;

    public BalanceService(PowerRealmsDbContext db)
    {
        _db = db;
    }

    public Task<MemberBalance?> GetBalanceAsync(Guid userId, Guid poolId, CancellationToken ct = default)
        => _db.MemberBalances.FirstOrDefaultAsync(m => m.UserId == userId && m.PoolId == poolId, ct);

    public async Task<MemberBalance> DepositAsync(Guid userId, Guid poolId, decimal amount, string description, CancellationToken ct = default)
    {
        var balance = await _db.MemberBalances.FirstOrDefaultAsync(m => m.UserId == userId && m.PoolId == poolId, ct);
        
        if (balance == null)
        {
            balance = new MemberBalance { UserId = userId, PoolId = poolId, Balance = amount };
            _db.MemberBalances.Add(balance);
        }
        else
        {
            balance.Balance += amount;
            _db.MemberBalances.Update(balance);
        }

        _db.BalanceTransactions.Add(new BalanceTransaction
        {
            UserId = userId,
            PoolId = poolId,
            Amount = amount,
            Type = "Deposit",
            Description = description
        });

        await _db.SaveChangesAsync(ct);
        return balance;
    }

    public async Task<bool> TransferAsync(Guid fromUserId, Guid toUserId, Guid poolId, decimal amount, string description, CancellationToken ct = default)
    {
        var fromBalance = await _db.MemberBalances.FirstOrDefaultAsync(m => m.UserId == fromUserId && m.PoolId == poolId, ct);
        if (fromBalance == null || fromBalance.Balance < amount)
            return false;

        var toBalance = await _db.MemberBalances.FirstOrDefaultAsync(m => m.UserId == toUserId && m.PoolId == poolId, ct);
        if (toBalance == null)
        {
            toBalance = new MemberBalance { UserId = toUserId, PoolId = poolId, Balance = 0 };
            _db.MemberBalances.Add(toBalance);
        }

        fromBalance.Balance -= amount;
        toBalance.Balance += amount;

        _db.BalanceTransactions.Add(new BalanceTransaction
        {
            UserId = fromUserId,
            PoolId = poolId,
            Amount = -amount,
            Type = "Transfer",
            Description = $"To {toUserId}: {description}"
        });

        _db.BalanceTransactions.Add(new BalanceTransaction
        {
            UserId = toUserId,
            PoolId = poolId,
            Amount = amount,
            Type = "Transfer",
            Description = $"From {fromUserId}: {description}"
        });

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IEnumerable<BalanceTransaction>> GetTransactionHistoryAsync(Guid userId, Guid poolId, CancellationToken ct = default)
        => await _db.BalanceTransactions
            .Where(t => t.UserId == userId && t.PoolId == poolId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);
}
