using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PowerRealms.Api.Services;
using PowerRealms.Api.Models;

namespace PowerRealms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NodesController : ControllerBase
{
    private readonly INodeService _nodes;
    private readonly ILocalizationService _localization;

    public NodesController(INodeService nodes, ILocalizationService localization)
    {
        _nodes = nodes;
        _localization = localization;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Register([FromBody] RegisterNodeDto dto)
    {
        var ownerId = Guid.Parse(User.FindFirst("id")!.Value);
        var node = new Node
        {
            OwnerId = ownerId,
            Name = dto.Name,
            CpuPower = dto.CpuPower,
            GpuPower = dto.GpuPower
        };
        var created = await _nodes.RegisterNodeAsync(node);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, new
        {
            message = _localization.GetMessage("General.Success"),
            node = created
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var node = await _nodes.GetNodeAsync(id);
        return node == null ? NotFound() : Ok(node);
    }

    [HttpGet("my")]
    [Authorize]
    public async Task<IActionResult> GetMyNodes()
    {
        var userId = Guid.Parse(User.FindFirst("id")!.Value);
        var nodes = await _nodes.GetUserNodesAsync(userId);
        return Ok(nodes);
    }

    [HttpGet("available")]
    public async Task<IActionResult> GetAvailable()
    {
        var nodes = await _nodes.GetAvailableNodesAsync();
        return Ok(nodes);
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateNodeDto dto)
    {
        var node = await _nodes.UpdateNodeAsync(id, dto.CpuPower, dto.GpuPower);
        return node == null ? NotFound() : Ok(new
        {
            message = _localization.GetMessage("General.Success"),
            node
        });
    }
}

public record RegisterNodeDto(string Name, double CpuPower, double GpuPower);
public record UpdateNodeDto(double? CpuPower, double? GpuPower);
