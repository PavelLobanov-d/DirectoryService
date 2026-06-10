using System.Reflection.Metadata;
using Microsoft.AspNetCore.Mvc;
using DirectoryService.Contracts.Locations;
using DirectoryService.Core.Locations;

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
        var locationId = await _locationsService.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        return Ok($"Location.Create : {locationId}");
    }
    [HttpGet]
    public async Task<IActionResult> GetAsync(
        [FromQuery] GetLocationsDto request,
        CancellationToken cancellationToken)
    {
        var locations = await _locationsService.GetAsync(request, cancellationToken).ConfigureAwait(false);
        return Ok($"Location.Get : {locations.Count}");
    }
    [HttpGet("{locationId:guid}")]
    public async Task<IActionResult> GetByIdAsync(
        [FromRoute] Guid locationId,
        CancellationToken cancellationToken)
    {
        var location = await _locationsService.GetByIdAsync(locationId, cancellationToken).ConfigureAwait(false);
        return Ok($"Location.GetByIdAsync : {location.Id}");
    }
    [HttpPut("{locationId:guid}")]
    public async Task<IActionResult> UpdateAsync(
        [FromRoute] Guid requestId,
        [FromBody] UpdateLocationDto request,
        CancellationToken cancellationToken)
    {
        bool result = await _locationsService.UpdateAsync(request, cancellationToken).ConfigureAwait(false);
        return Ok($"Location.Update : {result}");
    }
    [HttpDelete("{locationId:guid}")]
    public async Task<IActionResult> DeleteAsync(
        [FromRoute] Guid locationId,
        CancellationToken cancellationToken)
    {
        bool result = await _locationsService.DeleteAsync(locationId, cancellationToken).ConfigureAwait(false);
        return Ok($"Location.Delete : {result}");
    }
}
