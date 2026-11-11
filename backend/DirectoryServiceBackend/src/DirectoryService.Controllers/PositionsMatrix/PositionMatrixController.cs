using DirectoryService.Contracts;
using DirectoryService.Contracts.PositionsMatrix;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.PositionsMatrix;
using DirectoryService.Core.PositionsMatrix.ChangeParent;
using DirectoryService.Core.PositionsMatrix.Create;
using DirectoryService.Core.PositionsMatrix.Delete;
using DirectoryService.Core.PositionsMatrix.Get;
using DirectoryService.Core.PositionsMatrix.GetById;
using DirectoryService.Core.PositionsMatrix.GetByParentId;
using DirectoryService.Core.PositionsMatrix.Rename;
using DirectoryService.Core.PositionsMatrix.Update;
using DirectoryService.Domain.PositionsMatrix;
using DirectoryService.Shared;
using Microsoft.AspNetCore.Mvc;

namespace DirectoryService.Controllers.PositionsMatrix;


[ApiController]
[Route("positions")]
public class PositionMatrixController : ControllerBase
{
    private readonly IPositionMatrixService _positionMatrixService;
    public PositionMatrixController(IPositionMatrixService positionMatrixService)
    {
        _positionMatrixService = positionMatrixService;
    }

    [HttpPost]
    [ProducesResponseType<Envelope<CreatedObjectGuid>>(201)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(404)]
    [ProducesResponseType<Envelope>(409)]
    public async Task<EndPointResult<CreatedObjectGuid>> CreateAsync(
        [FromServices] ICommandHandler<CreatedObjectGuid, PositionMatrixCreateCommand> handler,
        [FromBody] CreatePositionMatrixDto request,
        CancellationToken cancellationToken)
    {
        var command = new PositionMatrixCreateCommand(request);
        return await handler.ExecuteAsync(command, cancellationToken).ConfigureAwait(false);
    }

    [HttpGet]
    [ProducesResponseType<Envelope<List<PositionMatrixDto>>>(200)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(404)]
    public async Task<EndPointResult<List<PositionMatrixDto>>> GetAsync(
        [FromServices] ICommandHandler<List<PositionMatrixDto>, PositionMatrixGetCommand> handler,
        [FromQuery] SelectDto request,
        CancellationToken cancellationToken)
    {
        var command = new PositionMatrixGetCommand(request);
        return await handler.ExecuteAsync(command, cancellationToken).ConfigureAwait(false);
    }

    [HttpGet("{positionMatrixId:guid}")]
    [ProducesResponseType<Envelope<PositionMatrix>>(200)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(404)]
    public async Task<EndPointResult<PositionMatrix?>> GetByIdAsync(
        [FromServices] ICommandHandler<PositionMatrix?, PositionMatrixGetByIdCommand> handler,
        [FromRoute] Guid positionMatrixId,
        CancellationToken cancellationToken)
    {
        var command = new PositionMatrixGetByIdCommand(positionMatrixId);
        return await handler.ExecuteAsync(command, cancellationToken).ConfigureAwait(false);
    }

    [HttpPatch("{positionMatrixId:guid}")]
    [ProducesResponseType<Envelope<bool>>(200)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(400)]
    [ProducesResponseType<Envelope>(404)]
    [ProducesResponseType<Envelope>(409)]
    public async Task<EndPointResult<bool>> RenameAsync(
        [FromServices] ICommandHandler<bool, RenamePositionCommand> handler,
        [FromRoute] Guid positionMatrixId,
        [FromBody] RenamePositionDto request,
        CancellationToken cancellationToken)
    {
        var command = new RenamePositionCommand(positionMatrixId, request);
        return await handler.ExecuteAsync(command, cancellationToken).ConfigureAwait(false);
    }

    [HttpPut("{positionMatrixId:guid}")]
    [ProducesResponseType<Envelope<bool>>(200)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(404)]
    [ProducesResponseType<Envelope>(409)]
    public async Task<EndPointResult<bool>> UpdateAsync(
        [FromServices] ICommandHandler<bool, PositionMatrixUpdateCommand> handler,
        [FromRoute] Guid positionMatrixId,
        [FromBody] UpdatePositionMatrixDto request,
        CancellationToken cancellationToken)
    {
        var command = new PositionMatrixUpdateCommand(positionMatrixId, request);
        return await handler.ExecuteAsync(command, cancellationToken).ConfigureAwait(false);
    }

    [HttpPut("move/{positionMatrixId:guid}")]
    [ProducesResponseType<Envelope<bool>>(200)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(404)]
    [ProducesResponseType<Envelope>(409)]
    public async Task<EndPointResult<bool>> Move(
        [FromServices] ICommandHandler<bool, PositionMatrixChangeParentCommand> handler,
        [FromRoute] Guid positionMatrixId,
        [FromBody] Guid newParentId,
        CancellationToken cancellationToken)
    {
        var command = new PositionMatrixChangeParentCommand(positionMatrixId, newParentId);
        return await handler.ExecuteAsync(command, cancellationToken).ConfigureAwait(false);
    }

    [HttpDelete("{positionMatrixId:guid}")]
    [ProducesResponseType<Envelope<bool>>(200)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(404)]
    public async Task<EndPointResult<bool>> Delete(
        [FromServices] ICommandHandler<bool, PositionMatrixDeleteCommand> handler,
        [FromRoute] Guid positionMatrixId,
        CancellationToken cancellationToken)
    {
        var command = new PositionMatrixDeleteCommand(positionMatrixId);
        return await handler.ExecuteAsync(command, cancellationToken).ConfigureAwait(false);
    }
}
