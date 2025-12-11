namespace PowerRealms.Api.Models;

public enum DisputeStatus { Open, InReview, Resolved, Closed }
public enum DisputeOutcome { FullRefund, PartialRefund, PayoutToNode, Dismissed }

public class Dispute
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PoolId { get; set; }
    public Guid InitiatorId { get; set; }
    public Guid? SessionId { get; set; }
    public Guid? OfferId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Evidence { get; set; }
    public DisputeStatus Status { get; set; } = DisputeStatus.Open;
    public DisputeOutcome? Outcome { get; set; }
    public Guid? ResolverId { get; set; }
    public string? Resolution { get; set; }
    public decimal? RefundAmount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
}
