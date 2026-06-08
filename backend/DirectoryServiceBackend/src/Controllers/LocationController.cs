using System.Reflection.Metadata;
using Microsoft.AspNetCore.Mvc;
using DirectoryService.Contracts.Locations;

namespace DirectoryService.Controller;


[ApiController]
[Route("[controller]")]
public class LocationController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateLocationDto request,
        CancellationToken cancellationToken)
    {
        return Ok("Location.Create");
    }
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] GetLocationsDto request,
        CancellationToken cancellationToken)
    {
        return Ok("Location.Get");
    }
    [HttpGet("{locationId:guid}")]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid requestId,
        CancellationToken cancellationToken)
    {
        return Ok("Location.GetById");
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
