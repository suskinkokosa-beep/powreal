namespace PowerRealms.Desktop.Models;

public enum PoolType { Public, Private, Referral }
public enum UserRole { Member, Officer, Owner, GlobalAdmin }
public enum HoldType { SessionHold, WithdrawalHold }
public enum HoldStatus { Active, Released, Forfeited }
public enum SessionStatus { Active, Completed, Cancelled }
public enum TransactionStatus { Pending, Completed, Failed }
public enum WithdrawalStatus { Pending, Confirmed, Completed, Rejected }

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Member;
    public bool IsGlobalAdmin { get; set; }
}

public class Pool
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public PoolType Type { get; set; }
    public Guid OwnerId { get; set; }
    public string? Password { get; set; }
    public string? MemberIdsJson { get; set; }
}

public class Node
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OwnerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public double CpuPower { get; set; }
    public double GpuPower { get; set; }
    public double Rating { get; set; } = 5.0;
}

public class PoolMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PoolId { get; set; }
    public Guid UserId { get; set; }
    public UserRole Role { get; set; }
    public int MachinesLimit { get; set; }
    public int MachinesUsed { get; set; }
    public int ReferralCount { get; set; }
}

public class PoolLedgerEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PoolId { get; set; }
    public Guid FromUserId { get; set; }
    public Guid ToUserId { get; set; }
    public decimal Amount { get; set; }
    public string Memo { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;
}

public class Hold
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PoolId { get; set; }
    public Guid FromUserId { get; set; }
    public decimal Amount { get; set; }
    public HoldType Type { get; set; }
    public HoldStatus Status { get; set; } = HoldStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class MemberBalance
{
    public Guid UserId { get; set; }
    public Guid PoolId { get; set; }
    public decimal Balance { get; set; }
}

public class Offer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SellerId { get; set; }
    public Guid PoolId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Payload { get; set; } = string.Empty;
}

public class GameSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PoolId { get; set; }
    public Guid NodeId { get; set; }
    public Guid SeekerId { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public SessionStatus Status { get; set; } = SessionStatus.Active;
    public decimal PricePerMinute { get; set; }
    public int MinutesReserved { get; set; }
    public decimal TotalHeld { get; set; }
    public double AvgLatencyMs { get; set; }
    public double AvgFps { get; set; }
    public double UptimePercent { get; set; }
    public decimal PayoutAmount { get; set; }
}

public class GameMetric
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public double LatencyMs { get; set; }
    public double Fps { get; set; }
    public double UptimeFraction { get; set; }
}

public class WithdrawalRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PoolId { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string ExternalAddress { get; set; } = string.Empty;
    public WithdrawalStatus Status { get; set; } = WithdrawalStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Memo { get; set; }
    public Guid? HoldId { get; set; }
}
