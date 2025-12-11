using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PowerRealms.Api.Services;
using PowerRealms.Api.Models;

namespace PowerRealms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PoolsController : ControllerBase
{
    private readonly IPoolService _pools;
    private readonly IPoolManagementService _management;
    private readonly ILocalizationService _localization;

    public PoolsController(IPoolService pools, IPoolManagementService management, ILocalizationService localization)
    {
        _pools = pools;
        _management = management;
        _localization = localization;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreatePoolDto dto)
    {
        var userId = Guid.Parse(User.FindFirst("id")!.Value);
        var pool = new Pool
        {
            Name = dto.Name,
            Type = dto.Type,
            OwnerId = userId,
            Password = dto.Password
        };
        var created = await _pools.CreatePoolAsync(pool);
        await _management.JoinPool(created.Id, userId);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, new
        {
            message = _localization.GetMessage("Pool.Created"),
            pool = created
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var pool = await _pools.GetPoolAsync(id);
        if (pool == null)
            return NotFound(new { message = _localization.GetMessage("Pool.NotFound") });
        return Ok(pool);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var pools = await _pools.GetAllPoolsAsync();
        return Ok(pools);
    }

    [HttpPost("{poolId}/join")]
    [Authorize]
    public async Task<IActionResult> Join(Guid poolId, [FromBody] JoinPoolDto? dto = null)
    {
        var userId = Guid.Parse(User.FindFirst("id")!.Value);
        var pool = await _pools.GetPoolAsync(poolId);
        
        if (pool == null)
            return NotFound(new { message = _localization.GetMessage("Pool.NotFound") });

        if (pool.Type == PoolType.Private && pool.Password != dto?.Password)
            return BadRequest(new { message = _localization.GetMessage("Pool.InvalidPassword") });

        var result = await _management.JoinPool(poolId, userId);
        if (result)
            return Ok(new { message = _localization.GetMessage("Pool.Joined") });
        return BadRequest(new { message = _localization.GetMessage("Pool.AlreadyMember") });
    }

    [HttpPost("{poolId}/promote/{userId}")]
    [Authorize(Roles = "Owner,GlobalAdmin")]
    public async Task<IActionResult> Promote(Guid poolId, Guid userId)
    {
        var result = await _management.PromoteToOfficer(poolId, userId);
        return result ? Ok(new { message = _localization.GetMessage("General.Success") }) 
                      : NotFound(new { message = _localization.GetMessage("General.NotFound") });
    }

    [HttpPost("{poolId}/machine")]
    [Authorize]
    public async Task<IActionResult> AddMachine(Guid poolId)
    {
        var userId = Guid.Parse(User.FindFirst("id")!.Value);
        var result = await _management.AddMachine(poolId, userId);
        if (result)
            return Ok(new { message = _localization.GetMessage("General.Success") });
        return BadRequest(new { message = _localization.GetMessage("Pool.MachineLimit") });
    }
}

public record CreatePoolDto(string Name, PoolType Type = PoolType.Public, string? Password = null);
public record JoinPoolDto(string? Password = null);
