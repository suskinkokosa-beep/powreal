using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PowerRealms.Api.Services;
using PowerRealms.Api.Models;

namespace PowerRealms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DisputesController : ControllerBase
{
    private readonly IDisputeService _disputes;
    private readonly ILocalizationService _localization;

    public DisputesController(IDisputeService disputes, ILocalizationService localization)
    {
        _disputes = disputes;
        _localization = localization;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateDisputeDto dto)
    {
        var userId = Guid.Parse(User.FindFirst("id")!.Value);
        var dispute = new Dispute
        {
            PoolId = dto.PoolId,
            InitiatorId = userId,
            SessionId = dto.SessionId,
            OfferId = dto.OfferId,
            Reason = dto.Reason,
            Evidence = dto.Evidence
        };
        var created = await _disputes.CreateDisputeAsync(dispute);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, new
        {
            message = _localization.GetMessage("General.Success"),
            dispute = created
        });
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> Get(Guid id)
    {
        var dispute = await _disputes.GetDisputeAsync(id);
        return dispute == null ? NotFound() : Ok(dispute);
    }

    [HttpGet("pool/{poolId}")]
    [Authorize]
    public async Task<IActionResult> GetPoolDisputes(Guid poolId)
    {
        var disputes = await _disputes.GetPoolDisputesAsync(poolId);
        return Ok(disputes);
    }

    [HttpPost("{id}/resolve")]
    [Authorize(Roles = "Owner,Officer,GlobalAdmin")]
    public async Task<IActionResult> Resolve(Guid id, [FromBody] ResolveDisputeDto dto)
    {
        var resolverId = Guid.Parse(User.FindFirst("id")!.Value);
        var result = await _disputes.ResolveDisputeAsync(id, resolverId, dto.Outcome, dto.Resolution, dto.RefundAmount);
        return result == null ? NotFound() : Ok(new
        {
            message = _localization.GetMessage("General.Success"),
            dispute = result
        });
    }
}

public record CreateDisputeDto(Guid PoolId, string Reason, Guid? SessionId = null, Guid? OfferId = null, string? Evidence = null);
public record ResolveDisputeDto(DisputeOutcome Outcome, string Resolution, decimal? RefundAmount = null);
