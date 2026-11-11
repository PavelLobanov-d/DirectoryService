using CSharpFunctionalExtensions;
using DirectoryService.Contracts;
using DirectoryService.Contracts.Locations;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Departments.Create;
using DirectoryService.Core.Locations;
using DirectoryService.Core.Locations.Create;
using DirectoryService.Core.Locations.Delete;
using DirectoryService.Core.Locations.Get;
using DirectoryService.Core.Locations.GetById;
using DirectoryService.Core.Locations.GetByIds;
using DirectoryService.Core.Locations.Update;
using DirectoryService.Domain.Locations;
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
    [ProducesResponseType<Envelope<List<Location>>>(200)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(404)]
    public async Task<EndPointResult<List<Location>>> GetAsync(
        [FromServices] ICommandHandler<List<Location>, LocationGetCommand> handler,
        [FromQuery] SelectDto request,
        CancellationToken cancellationToken)
    {
        var command = new LocationGetCommand(request);
        return await handler.ExecuteAsync(command, cancellationToken).ConfigureAwait(false);
    }

    [HttpGet("{locationId:guid}")]
    [ProducesResponseType<Envelope<Location>>(200)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(404)]
    public async Task<EndPointResult<Location?>> GetByIdAsync(
        [FromServices] ICommandHandler<Location?, LocationGetByIdCommand> handler,
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
