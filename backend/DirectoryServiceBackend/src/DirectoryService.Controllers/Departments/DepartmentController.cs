using CSharpFunctionalExtensions;
using DirectoryService.Contracts;
using DirectoryService.Contracts.Departments;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Departments;
using DirectoryService.Core.Departments.ChangeParent;
using DirectoryService.Core.Departments.Create;
using DirectoryService.Core.Departments.Delete;
using DirectoryService.Core.Departments.DetachLocation;
using DirectoryService.Core.Departments.DetachPosition;
using DirectoryService.Core.Departments.Get;
using DirectoryService.Core.Departments.GetById;
using DirectoryService.Core.Departments.LinkLocation;
using DirectoryService.Core.Departments.LinkPosition;
using DirectoryService.Core.Departments.Update;
using DirectoryService.Domain.Departments;
using DirectoryService.Shared;
using Microsoft.AspNetCore.Mvc;

namespace DirectoryService.Controllers.Departments;


[ApiController]
[Route("departments")]
public class DepartmentController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<Envelope<CreatedObjectGuid>>(201)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(400)]
    [ProducesResponseType<Envelope>(404)]
    [ProducesResponseType<Envelope>(409)]
    public async Task<EndPointResult<CreatedObjectGuid>> CreateAsync(
        [FromServices] ICommandHandler<CreatedObjectGuid, CreateDepartmentCommand> handler,
        [FromBody] CreateDepartmentDto request,
        CancellationToken cancellationToken)
    {
        var command = new CreateDepartmentCommand(request);
        return await handler.ExecuteAsync(command, cancellationToken).ConfigureAwait(false);
    }
    [HttpGet]
    [ProducesResponseType<List<Department>> (200)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(404)]
    public async Task<EndPointResult<List<Department>>> GetAsync(
        [FromServices] ICommandHandler<List<Department>, GetDepartmentCommand> handler,
        [FromQuery] SelectDto request,
        CancellationToken cancellationToken)
    {
        var command = new GetDepartmentCommand(request);
        return await handler.ExecuteAsync(command, cancellationToken).ConfigureAwait(false);
    }

    [HttpPatch("{departmentId:guid}/locations/{locationId:guid}")]
    [ProducesResponseType<Envelope<CreatedObjectGuid>>(201)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(400)]
    [ProducesResponseType<Envelope>(404)]
    [ProducesResponseType<Envelope>(409)]
    public async Task<EndPointResult<CreatedObjectGuid>> LinkLocationAsync(
        [FromServices] ICommandHandler<CreatedObjectGuid, DepartmentLinkLocationCommand> handler,
        [FromRoute] Guid departmentId,
        [FromRoute] Guid locationId,
        CancellationToken cancellationToken)
    {
        var command = new DepartmentLinkLocationCommand(departmentId, locationId);
        return await handler.ExecuteAsync(command, cancellationToken).ConfigureAwait(false);
    }

    [HttpDelete("{departmentId:guid}/locations/{locationId:guid}")]
    [ProducesResponseType<Envelope<List<Department>>>(200)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(404)]
    public async Task<EndPointResult<bool>> DetachLocationAsync(
        [FromServices] ICommandHandler<bool, DepartmentDetachLocationCommand> handler,
        [FromRoute] Guid departmentId,
        [FromRoute] Guid locationId,
        CancellationToken cancellationToken)
    {
        var command = new DepartmentDetachLocationCommand(departmentId, locationId);
        return await handler.ExecuteAsync(command, cancellationToken).ConfigureAwait(false);
    }

    [HttpGet("{departmentId:guid}")]
    [ProducesResponseType<Envelope<List<Department>>>(200)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(404)]
    public async Task<EndPointResult<Department?>> GetByIdAsync(
        [FromServices] ICommandHandler<Department?, GetDepartmentByIdCommand> handler,
        [FromRoute] Guid departmentId,
        CancellationToken cancellationToken)
    {
        var command = new GetDepartmentByIdCommand(departmentId);
        return await handler.ExecuteAsync(command, cancellationToken).ConfigureAwait(false);
    }

    [HttpPut("{departmentId:guid}")]
    [ProducesResponseType<Envelope<List<Department>>>(200)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(400)]
    [ProducesResponseType<Envelope>(404)]
    [ProducesResponseType<Envelope>(409)]
    public async Task<EndPointResult<bool>> UpdateAsync(
        [FromServices] ICommandHandler<bool, UpdateDepartmentCommand> handler,
        [FromRoute] Guid departmentId,
        [FromBody] UpdateDepartmentDto request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateDepartmentCommand(departmentId, request);
        return await handler.ExecuteAsync(command, cancellationToken).ConfigureAwait(false);
    }

    [HttpDelete("{departmentId:guid}")]
    [ProducesResponseType<Envelope<List<Department>>>(200)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(404)]
    public async Task<EndPointResult<bool>> DeleteAsync(
        [FromServices] ICommandHandler<bool, DeleteDepartmentCommand> handler,
        [FromRoute] Guid departmentId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteDepartmentCommand(departmentId);
        return await handler.ExecuteAsync(command, cancellationToken).ConfigureAwait(false);
    }

    [HttpPut("move/{departmentId:guid}")]
    [ProducesResponseType<Envelope<List<Department>>>(200)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(404)]
    [ProducesResponseType<Envelope>(409)]
    public async Task<EndPointResult<bool>> MoveAsync(
        [FromServices] ICommandHandler<bool, ChangeParentDepartmentCommand> handler,
        [FromRoute] Guid departmentId,
        [FromBody] Guid newDepartmentId,
        CancellationToken cancellationToken)
    {
        var command = new ChangeParentDepartmentCommand(departmentId, newDepartmentId);
        return await handler.ExecuteAsync(command, cancellationToken).ConfigureAwait(false);
    }

    [HttpPost("{departmentId:guid}/positions/{positionMatrixId:guid}")]
    [ProducesResponseType<Envelope<CreatedObjectGuid>>(201)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(404)]
    [ProducesResponseType<Envelope>(409)]
    public async Task<EndPointResult<CreatedObjectGuid>> LinkPosition(
        [FromServices] ICommandHandler<CreatedObjectGuid, DepartmentLinkPositionCommand> handler,
        [FromRoute] Guid departmentId,
        [FromRoute] Guid positionMatrixId,
        CancellationToken cancellationToken)
    {
        var command = new DepartmentLinkPositionCommand(departmentId, positionMatrixId);
        return await handler.ExecuteAsync(command, cancellationToken).ConfigureAwait(false);
    }

    [HttpDelete("{departmentId:guid}/positions/{positionMatrixId:guid}")]
    [ProducesResponseType<Envelope<bool>>(200)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(404)]
    public async Task<EndPointResult<bool>> DetachPosition(
        [FromServices] ICommandHandler<bool, DepartmentDetachPositionCommand> handler,
        [FromRoute] Guid departmentId,
        [FromRoute] Guid positionMatrixId,
        CancellationToken cancellationToken)
    {
        var command = new DepartmentDetachPositionCommand(departmentId, positionMatrixId);
        return await handler.ExecuteAsync(command, cancellationToken).ConfigureAwait(false);
    }
}
