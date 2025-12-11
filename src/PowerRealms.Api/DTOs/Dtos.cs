namespace PowerRealms.Api.DTOs;
public record CreatePoolDto(System.String Name, System.String? Password, System.String Type);
public record CreateOfferDto(System.Guid PoolId, string Title, decimal Price, string Payload);
public record CreateHoldDto(System.Guid PoolId, System.Guid FromUserId, decimal Amount, string Type);
