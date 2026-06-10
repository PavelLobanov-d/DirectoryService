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
        return Ok($"Location.CreateAsync : {locationId}");
    }
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] GetLocationsDto request,
        CancellationToken cancellationToken)
    {
        return Ok("Location.Get");
    }
    [HttpGet("{locationId:guid}")]
    public async Task<IActionResult> GetByIdAsync(
        [FromRoute] Guid requestId,
        CancellationToken cancellationToken)
    {
        var location = await _locationsService.GetByIdAsync(requestId, cancellationToken).ConfigureAwait(false);
        return Ok($"Location.GetByIdAsync : {location.Id}");
    }
    [HttpPut("{locationId:guid}")]
    public async Task<IActionResult> Update(
        [FromRoute] Guid requestId,
        [FromBody] UpdateLocationDto request,
        CancellationToken cancellationToken)
    {
        return Ok("Location.Update");
    }
    [HttpDelete("{locationId:guid}")]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid requestId,
        CancellationToken cancellationToken)
    {
        return Ok("Location.Delete");
    }
}
