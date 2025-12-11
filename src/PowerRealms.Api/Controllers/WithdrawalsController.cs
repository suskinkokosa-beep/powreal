using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PowerRealms.Api.Services;
using PowerRealms.Api.Models;

namespace PowerRealms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WithdrawalsController : ControllerBase
{
    private readonly IWithdrawalService _withdrawal;
    public WithdrawalsController(IWithdrawalService withdrawal) => _withdrawal = withdrawal;

    [HttpPost("request")]
    [Authorize]
    public async Task<IActionResult> Request([FromBody] WithdrawalRequestDto dto)
    {
        var userId = Guid.Parse(User.FindFirst("id")!.Value);
        var req = await _withdrawal.CreateWithdrawalRequestAsync(dto.PoolId, userId, dto.Amount, dto.ExternalAddress);
        return CreatedAtAction(nameof(Get), new { id = req.Id }, req);
    }

    [HttpPost("confirm/{id}")]
    [Authorize(Roles = "Owner,Officer,GlobalAdmin")]
    public async Task<IActionResult> Confirm(Guid id, [FromBody] ConfirmDto dto)
    {
        var confirmerId = Guid.Parse(User.FindFirst("id")!.Value);
        var result = await _withdrawal.ConfirmWithdrawalAsync(id, confirmerId, dto.Approved);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpGet("pool/{poolId}")]
    [Authorize]
    public async Task<IActionResult> ForPool(Guid poolId)
    {
        var list = await _withdrawal.GetPoolWithdrawalsAsync(poolId);
        return Ok(list);
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> Get(Guid id)
    {
        var req = await _withdrawal.GetAsync(id);
        return req == null ? NotFound() : Ok(req);
    }
}

public record WithdrawalRequestDto(Guid PoolId, decimal Amount, string ExternalAddress);
public record ConfirmDto(bool Approved);
