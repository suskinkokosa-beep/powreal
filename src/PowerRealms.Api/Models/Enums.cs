namespace PowerRealms.Api.Models;
public enum TransactionStatus { Pending, Released, Completed, Cancelled }
public enum HoldType { Payment, Withdrawal }
public enum PoolType { Public, Private, Premium }
public enum UserRole { GlobalAdmin, Owner, Officer, Member }
public enum PoolMemberRole { Owner, Officer, Member }
public enum GameSessionStatus { Pending, Active, Ended, Cancelled }
