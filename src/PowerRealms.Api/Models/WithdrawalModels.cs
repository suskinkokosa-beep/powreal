using System;

namespace PowerRealms.Api.Models;

public enum WithdrawalStatus { Requested, Confirmed, Completed, Rejected }

public class WithdrawalRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PoolId { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string ExternalAddress { get; set; } = string.Empty;
    public WithdrawalStatus Status { get; set; } = WithdrawalStatus.Requested;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Memo { get; set; }
    public Guid? HoldId { get; set; }
}
