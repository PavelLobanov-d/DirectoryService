using CSharpFunctionalExtensions;
using DirectoryService.Contracts;
using DirectoryService.Contracts.Locations;
using DirectoryService.Core.Locations;
using DirectoryService.Domain.Departments;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.shared;
using Microsoft.AspNetCore.Mvc;
using System.Reflection.Metadata;

namespace DirectoryService.Controllers.Locations;


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
    [ProducesResponseType<Envelope<CreatedObjectGuid>>(201)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(400)]
    [ProducesResponseType<Envelope>(404)]
    [ProducesResponseType<Envelope>(409)]
    public async Task<EndPointResult<CreatedObjectGuid>> CreateAsync(
        [FromBody] CreateLocationDto request,
        CancellationToken cancellationToken)
    {
        return await _locationsService.CreateAsync(request, cancellationToken).ConfigureAwait(false);
    }

    [HttpGet]
    [ProducesResponseType<Envelope<List<Location>>>(200)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(404)]
    public async Task<EndPointResult<List<Location>>> GetAsync(
        [FromQuery] SelectDto request,
        CancellationToken cancellationToken)
    {
        return await _locationsService.GetAsync(request, cancellationToken).ConfigureAwait(false);
    }

    [HttpGet("{locationId:guid}")]
    [ProducesResponseType<Envelope<Location>>(200)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(404)]
    public async Task<EndPointResult<Location?>> GetByIdAsync(
        [FromRoute] Guid locationId,
        CancellationToken cancellationToken)
    {
        return await _locationsService.GetByIdAsync(locationId, cancellationToken).ConfigureAwait(false);
    }

    [HttpPut("{locationId:guid}")]
    [ProducesResponseType<Envelope<bool>>(200)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(400)]
    [ProducesResponseType<Envelope>(404)]
    [ProducesResponseType<Envelope>(409)]
    public async Task<EndPointResult<bool>> UpdateAsync(
        [FromRoute] Guid locationId,
        [FromBody] UpdateLocationDto request,
        CancellationToken cancellationToken)
    {
        return await _locationsService.UpdateAsync(locationId, request, cancellationToken).ConfigureAwait(false);
    }

    [HttpDelete("{locationId:guid}")]
    [ProducesResponseType<Envelope<bool>>(200)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(404)]
    public async Task<EndPointResult<bool>> DeleteAsync(
        [FromRoute] Guid locationId,
        CancellationToken cancellationToken)
    {
        return await _locationsService.DeleteAsync(locationId, cancellationToken).ConfigureAwait(false);
    }
}
