using DirectoryService.Contracts;
using DirectoryService.Contracts.Locations;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Locations.Commands.Create;
using DirectoryService.Core.Locations.Commands.Delete;
using DirectoryService.Core.Locations.Commands.Update;
using DirectoryService.Core.Locations.Queries.Get;
using DirectoryService.Core.Locations.Queries.GetById;
using DirectoryService.Core.Locations.Queries.GetTop;
using DirectoryService.Shared;
using Microsoft.AspNetCore.Mvc;

namespace DirectoryService.Controllers.Locations;


[ApiController]
[Route("locations")]
public class LocationController : ControllerBase
{    
    [HttpPost]
    [ProducesResponseType<Envelope<CreatedObjectGuid>>(201)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(400)]
    [ProducesResponseType<Envelope>(404)]
    [ProducesResponseType<Envelope>(409)]
    public async Task<EndPointResult<CreatedObjectGuid>> CreateAsync(
        [FromServices] ICommandHandler<CreatedObjectGuid, LocationCreateCommand> handler,
        [FromBody] CreateLocationDto request,
        CancellationToken cancellationToken)
    {
        var command = new LocationCreateCommand(request);
        return await handler.ExecuteAsync(command, cancellationToken).ConfigureAwait(false);
    }

    [HttpGet]
    [ProducesResponseType<Envelope<PagedResult<LocationListItemDto>>>(200)]
    [ProducesResponseType<Envelope>(400)]
    [ProducesResponseType<Envelope>(500)]
    public async Task<EndPointResult<PagedResult<LocationListItemDto>>> GetAsync(
        [FromServices] ICommandHandler<PagedResult<LocationListItemDto>, LocationGetCommand> handler,
        [FromQuery] GetLocationsRequest request,
        CancellationToken cancellationToken)
    {
        var command = new LocationGetCommand(request);
        return await handler.ExecuteAsync(command, cancellationToken).ConfigureAwait(false);
    }

    [HttpGet("top")]
    [ProducesResponseType<Envelope<List<GetLocationsByDepartmentsCount>>>(200)]
    [ProducesResponseType<Envelope>(500)]
    public async Task<EndPointResult<List<GetLocationsByDepartmentsCount>>> GetTopAsync(
        [FromServices] ICommandHandler<List<GetLocationsByDepartmentsCount>, GetLocationsTopCommand> handler,
        CancellationToken cancellationToken)
    {
        var command = new GetLocationsTopCommand();
        return await handler.ExecuteAsync(command, cancellationToken).ConfigureAwait(false);
    }

    [HttpGet("{locationId:guid}")]
    [ProducesResponseType<Envelope<GetLocationResponse>>(200)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(404)]
    public async Task<EndPointResult<GetLocationResponse>> GetByIdAsync(
        [FromServices] ICommandHandler<GetLocationResponse, LocationGetByIdCommand> handler,
        [FromRoute] Guid locationId,
        CancellationToken cancellationToken)
    {
        var command = new LocationGetByIdCommand(locationId);
        return await handler.ExecuteAsync(command, cancellationToken).ConfigureAwait(false);
    }

    [HttpPut("{locationId:guid}")]
    [ProducesResponseType<Envelope<bool>>(200)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(400)]
    [ProducesResponseType<Envelope>(404)]
    [ProducesResponseType<Envelope>(409)]
    public async Task<EndPointResult<bool>> UpdateAsync(
        [FromServices] ICommandHandler<bool, LocationUpdateCommand> handler,
        [FromRoute] Guid locationId,
        [FromBody] UpdateLocationDto request,
        CancellationToken cancellationToken)
    {
        var command = new LocationUpdateCommand(locationId, request);
        return await handler.ExecuteAsync(command, cancellationToken).ConfigureAwait(false);
    }

    [HttpDelete("{locationId:guid}")]
    [ProducesResponseType<Envelope<bool>>(200)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(404)]
    public async Task<EndPointResult<bool>> DeleteAsync(
        [FromServices] ICommandHandler<bool, LocationDeleteCommand> handler,
        [FromRoute] Guid locationId,
        CancellationToken cancellationToken)
    {
        var command = new LocationDeleteCommand(locationId);
        return await handler.ExecuteAsync(command, cancellationToken).ConfigureAwait(false);
    }
}
