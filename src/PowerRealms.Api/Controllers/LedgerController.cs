using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PowerRealms.Api.Services;
using PowerRealms.Api.Models;

namespace PowerRealms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LedgerController : ControllerBase
{
    private readonly ILedgerService _ledger;
    private readonly IHoldService _hold;
    private readonly ILocalizationService _localization;

    public LedgerController(ILedgerService ledger, IHoldService hold, ILocalizationService localization)
    {
        _ledger = ledger;
        _hold = hold;
        _localization = localization;
    }

    [HttpGet("pool/{poolId}")]
    [Authorize]
    public async Task<IActionResult> GetPoolLedger(Guid poolId)
    {
        var entries = await _ledger.GetPoolLedgerAsync(poolId);
        return Ok(entries);
    }

    [HttpPost("hold")]
    [Authorize]
    public async Task<IActionResult> CreateHold([FromBody] CreateHoldDto dto)
    {
        var userId = Guid.Parse(User.FindFirst("id")!.Value);
        var hold = new Hold
        {
            PoolId = dto.PoolId,
            FromUserId = userId,
            Amount = dto.Amount,
            Type = dto.Type
        };
        var created = await _hold.CreateHoldAsync(hold);
        return Ok(new
        {
            message = _localization.GetMessage("Hold.Created"),
            hold = created
        });
    }

    [HttpPost("hold/{holdId}/release")]
    [Authorize]
    public async Task<IActionResult> ReleaseHold(Guid holdId, [FromBody] ReleaseHoldDto dto)
    {
        var result = await _hold.ReleaseHoldAsync(holdId, dto.Success);
        if (result == null)
            return NotFound(new { message = _localization.GetMessage("General.NotFound") });

        var message = dto.Success 
            ? _localization.GetMessage("Hold.Released")
            : _localization.GetMessage("Hold.Cancelled");
        return Ok(new { message, hold = result });
    }
}

public record CreateHoldDto(Guid PoolId, decimal Amount, HoldType Type);
public record ReleaseHoldDto(bool Success);
