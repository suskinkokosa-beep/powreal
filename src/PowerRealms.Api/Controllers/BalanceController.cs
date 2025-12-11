using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PowerRealms.Api.Services;

namespace PowerRealms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BalanceController : ControllerBase
{
    private readonly IBalanceService _balance;
    private readonly ILocalizationService _localization;

    public BalanceController(IBalanceService balance, ILocalizationService localization)
    {
        _balance = balance;
        _localization = localization;
    }

    [HttpGet("pool/{poolId}")]
    [Authorize]
    public async Task<IActionResult> GetBalance(Guid poolId)
    {
        var userId = Guid.Parse(User.FindFirst("id")!.Value);
        var balance = await _balance.GetBalanceAsync(userId, poolId);
        return Ok(new { balance = balance?.Balance ?? 0m });
    }

    [HttpPost("deposit")]
    [Authorize]
    public async Task<IActionResult> Deposit([FromBody] DepositDto dto)
    {
        var userId = Guid.Parse(User.FindFirst("id")!.Value);
        var result = await _balance.DepositAsync(userId, dto.PoolId, dto.Amount, dto.Description ?? "Deposit");
        return Ok(new
        {
            message = _localization.GetMessage("General.Success"),
            balance = result.Balance
        });
    }

    [HttpPost("transfer")]
    [Authorize]
    public async Task<IActionResult> Transfer([FromBody] TransferDto dto)
    {
        var fromUserId = Guid.Parse(User.FindFirst("id")!.Value);
        var success = await _balance.TransferAsync(fromUserId, dto.ToUserId, dto.PoolId, dto.Amount, dto.Description ?? "Transfer");
        
        if (!success)
            return BadRequest(new { message = _localization.GetMessage("Withdrawal.InsufficientBalance") });
        
        return Ok(new { message = _localization.GetMessage("General.Success") });
    }

    [HttpGet("history/pool/{poolId}")]
    [Authorize]
    public async Task<IActionResult> GetHistory(Guid poolId)
    {
        var userId = Guid.Parse(User.FindFirst("id")!.Value);
        var transactions = await _balance.GetTransactionHistoryAsync(userId, poolId);
        return Ok(transactions);
    }
}

public record DepositDto(Guid PoolId, decimal Amount, string? Description = null);
public record TransferDto(Guid PoolId, Guid ToUserId, decimal Amount, string? Description = null);
