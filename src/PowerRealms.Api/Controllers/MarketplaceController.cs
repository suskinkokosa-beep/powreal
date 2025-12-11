using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PowerRealms.Api.Services;
using PowerRealms.Api.Models;

namespace PowerRealms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MarketplaceController : ControllerBase
{
    private readonly IMarketplaceService _marketplace;
    private readonly ILocalizationService _localization;

    public MarketplaceController(IMarketplaceService marketplace, ILocalizationService localization)
    {
        _marketplace = marketplace;
        _localization = localization;
    }

    [HttpPost("offer")]
    [Authorize]
    public async Task<IActionResult> CreateOffer([FromBody] CreateOfferDto dto)
    {
        var sellerId = Guid.Parse(User.FindFirst("id")!.Value);
        var offer = new Offer
        {
            SellerId = sellerId,
            PoolId = dto.PoolId,
            Title = dto.Title,
            Price = dto.Price,
            Payload = dto.Payload ?? ""
        };
        var created = await _marketplace.CreateOfferAsync(offer);
        return CreatedAtAction(nameof(GetPoolOffers), new { poolId = dto.PoolId }, new
        {
            message = _localization.GetMessage("Marketplace.OfferCreated"),
            offer = created
        });
    }

    [HttpGet("pool/{poolId}")]
    public async Task<IActionResult> GetPoolOffers(Guid poolId)
    {
        var offers = await _marketplace.GetOffersAsync(poolId);
        return Ok(offers);
    }
}

public record CreateOfferDto(Guid PoolId, string Title, decimal Price, string? Payload = null);
