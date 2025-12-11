using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PowerRealms.Api.Services;
using PowerRealms.Api.Models;

namespace PowerRealms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GameBoostController : ControllerBase
{
    private readonly IGameBoostService _gameboost;
    private readonly ILocalizationService _localization;

    public GameBoostController(IGameBoostService gameboost, ILocalizationService localization)
    {
        _gameboost = gameboost;
        _localization = localization;
    }

    [HttpPost("session/start")]
    [Authorize]
    public async Task<IActionResult> StartSession([FromBody] StartSessionDto dto)
    {
        var seekerId = Guid.Parse(User.FindFirst("id")!.Value);
        try
        {
            var session = await _gameboost.StartSessionAsync(dto.PoolId, dto.NodeId, seekerId, dto.Minutes);
            return Ok(new
            {
                message = _localization.GetMessage("GameBoost.SessionStarted"),
                session
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = _localization.GetMessage("GameBoost.NodeNotAvailable"), error = ex.Message });
        }
    }

    [HttpPost("session/{sessionId}/metrics")]
    [Authorize]
    public async Task<IActionResult> ReportMetrics(Guid sessionId, [FromBody] ReportMetricsDto dto)
    {
        var metric = new GameMetric
        {
            SessionId = sessionId,
            LatencyMs = dto.LatencyMs,
            Fps = dto.Fps,
            UptimeFraction = dto.UptimeFraction
        };
        var result = await _gameboost.ReportMetricsAsync(sessionId, metric);
        return result ? Ok(new { message = _localization.GetMessage("General.Success") }) 
                      : NotFound(new { message = _localization.GetMessage("General.NotFound") });
    }

    [HttpPost("session/{sessionId}/end")]
    [Authorize]
    public async Task<IActionResult> EndSession(Guid sessionId)
    {
        try
        {
            var session = await _gameboost.EndSessionAsync(sessionId);
            if (session == null)
                return NotFound(new { message = _localization.GetMessage("General.NotFound") });
            
            var response = new
            {
                message = _localization.GetMessage("GameBoost.SessionEnded"),
                session,
                qualityReport = new
                {
                    avgLatency = $"{session.AvgLatencyMs:F1} ms",
                    avgFps = $"{session.AvgFps:F1}",
                    uptime = $"{session.UptimePercent:F1}%",
                    payoutAmount = session.PayoutAmount,
                    refundAmount = session.TotalHeld - session.PayoutAmount
                }
            };
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("session/{sessionId}")]
    [Authorize]
    public async Task<IActionResult> GetSession(Guid sessionId)
    {
        var session = await _gameboost.GetSessionAsync(sessionId);
        return session == null ? NotFound(new { message = _localization.GetMessage("General.NotFound") }) : Ok(session);
    }
}

public record StartSessionDto(Guid PoolId, Guid NodeId, int Minutes);
public record ReportMetricsDto(double LatencyMs, double Fps, double UptimeFraction);
