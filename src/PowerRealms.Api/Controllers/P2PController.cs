using Microsoft.AspNetCore.Mvc;
using PowerRealms.Api.P2P;
using PowerRealms.Api.Services;

namespace PowerRealms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class P2PController : ControllerBase
{
    private readonly IP2PNode _p2pNode;
    private readonly ISyncService _syncService;
    private readonly ILocalizationService _localization;

    public P2PController(IP2PNode p2pNode, ISyncService syncService, ILocalizationService localization)
    {
        _p2pNode = p2pNode;
        _syncService = syncService;
        _localization = localization;
    }

    [HttpGet("info")]
    public async Task<IActionResult> GetNodeInfo()
    {
        var peers = await _p2pNode.GetPeersAsync();
        return Ok(new
        {
            nodeId = _p2pNode.NodeId,
            peersCount = peers.Count(),
            peers = peers
        });
    }

    [HttpPost("connect")]
    public async Task<IActionResult> ConnectToPeer([FromBody] ConnectPeerDto dto)
    {
        await _p2pNode.ConnectToPeerAsync(dto.Endpoint);
        return Ok(new { message = _localization.GetMessage("General.Success") });
    }

    [HttpGet("peers")]
    public async Task<IActionResult> GetPeers()
    {
        var peers = await _p2pNode.GetPeersAsync();
        return Ok(peers);
    }

    [HttpPost("sync")]
    public async Task<IActionResult> RequestSync()
    {
        await _syncService.RequestSyncAsync(Guid.Empty);
        return Ok(new { message = _localization.GetMessage("General.Success") });
    }

    [HttpGet("sync/package")]
    public async Task<IActionResult> GetSyncPackage([FromQuery] DateTime? since = null)
    {
        var package = await _syncService.GetSyncPackageAsync(since);
        return Ok(package);
    }
}

public record ConnectPeerDto(string Endpoint);
