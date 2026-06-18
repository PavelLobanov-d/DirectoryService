using System.Reflection.Metadata;
using Microsoft.AspNetCore.Mvc;
using DirectoryService.Contracts.Locations;
using DirectoryService.Core.Locations;
using DirectoryService.Domain.shared;

namespace DirectoryService.Controller;


[ApiController]
[Route("[controller]")]
public class LocationController : ControllerBase
{
    private readonly ILocationsService _locationsService;
    public LocationController(ILocationsService locationsService)
    {
        _locationsService = locationsService;
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateLocationDto request,
        CancellationToken cancellationToken)
    {
        var resultLocationId = await _locationsService.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        if (resultLocationId.IsFailure)
        {
            return BadRequest(resultLocationId.Error);
        }
        return Ok($"Location.Create : {resultLocationId.Value}");
    }
    [HttpGet]
    public async Task<IActionResult> GetAsync(
        [FromQuery] GetLocationsDto request,
        CancellationToken cancellationToken)
    {
        var resultLocations = await _locationsService.GetAsync(request, cancellationToken).ConfigureAwait(false);
        if (resultLocations.IsFailure)
        { 
            return BadRequest(resultLocations.Error);
        }
        return Ok($"Location.Get : {resultLocations.Value.Count}");
    }
    [HttpGet("{locationId:guid}")]
    public async Task<IActionResult> GetByIdAsync(
        [FromRoute] Guid locationId,
        CancellationToken cancellationToken)
    {
        var resultLocations = await _locationsService.GetByIdAsync(locationId, cancellationToken).ConfigureAwait(false);
        if (resultLocations.IsFailure)
        {
            return BadRequest(resultLocations.Error);
        }
        return Ok($"Location.GetByIdAsync : {resultLocations.Value}");
    }
    [HttpPut("{locationId:guid}")]
    public async Task<IActionResult> UpdateAsync(
        [FromRoute] Guid requestId,
        [FromBody] UpdateLocationDto request,
        CancellationToken cancellationToken)
    {
        var result = await _locationsService.UpdateAsync(request, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }
        return Ok($"Location.Update : {result.Value}");
    }
    [HttpDelete("{locationId:guid}")]
    public async Task<IActionResult> DeleteAsync(
        [FromRoute] Guid locationId,
        CancellationToken cancellationToken)
    {
        var result = await _locationsService.DeleteAsync(locationId, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }
        return Ok($"Location.Delete : {result.Value}");
    }
}
