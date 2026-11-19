using CSharpFunctionalExtensions;
using DirectoryService.Contracts;
using DirectoryService.Contracts.Departments;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Departments;
using DirectoryService.Core.Departments.Commands.ChangeParent;
using DirectoryService.Core.Departments.Commands.Create;
using DirectoryService.Core.Departments.Commands.Delete;
using DirectoryService.Core.Departments.Commands.DetachLocation;
using DirectoryService.Core.Departments.Commands.DetachPosition;
using DirectoryService.Core.Departments.Commands.LinkLocation;
using DirectoryService.Core.Departments.Commands.LinkPosition;
using DirectoryService.Core.Departments.Commands.Update;
using DirectoryService.Core.Departments.Queries.Get;
using DirectoryService.Core.Departments.Queries.GetById;
using DirectoryService.Core.Departments.Queries.GetByParentId;
using DirectoryService.Core.Departments.Queries.Tree;
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
    [ProducesResponseType<Envelope<GetDepartmentsResponse>>(200)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(404)]
    public async Task<EndPointResult<GetDepartmentsResponse>> GetAsync(
        [FromServices] ICommandHandler<GetDepartmentsResponse, GetDepartmentCommand> handler,
        [FromQuery] SelectDto request,
        CancellationToken cancellationToken)
    {
        var command = new GetDepartmentCommand(request);
        return await handler.ExecuteAsync(command, cancellationToken).ConfigureAwait(false);
    }

    [HttpGet("dapper")]
    [ProducesResponseType<Envelope<GetDepartmentsResponse>>(200)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(400)]
    public async Task<EndPointResult<GetDepartmentsResponse>> GetDapperAsync(
        [FromServices] ICommandHandler<GetDepartmentsResponse, GetDepartmentsDapperCommand> handler,
        [FromQuery] SelectDto request,
        CancellationToken cancellationToken)
    {
        var command = new GetDepartmentsDapperCommand(request);
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
    [ProducesResponseType<Envelope<GetDepartmentResponse>>(200)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(404)]
    public async Task<EndPointResult<GetDepartmentResponse?>> GetByIdAsync(
        [FromServices] ICommandHandler<GetDepartmentResponse?, GetDepartmentByIdCommand> handler,
        [FromRoute] Guid departmentId,
        CancellationToken cancellationToken)
    {
        var command = new GetDepartmentByIdCommand(departmentId);
        return await handler.ExecuteAsync(command, cancellationToken).ConfigureAwait(false);
    }

    [HttpGet("{departmentId:guid}/children")]
    [ProducesResponseType<Envelope<List<DepartmentTreeItemResponse>>>(200)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(404)]
    public async Task<EndPointResult<List<DepartmentTreeItemResponse>>> GetChildrenAsync(
        [FromServices] ICommandHandler<List<DepartmentTreeItemResponse>, GetDepartmentChildrenCommand> handler,
        [FromRoute] Guid departmentId,
        CancellationToken cancellationToken)
    {
        var command = new GetDepartmentChildrenCommand(departmentId);
        return await handler.ExecuteAsync(command, cancellationToken).ConfigureAwait(false);
    }

    [HttpGet("{departmentId:guid}/ancestors")]
    [ProducesResponseType<Envelope<List<DepartmentAncestorResponse>>>(200)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(404)]
    public async Task<EndPointResult<List<DepartmentAncestorResponse>>> GetAncestorsAsync(
        [FromServices] ICommandHandler<List<DepartmentAncestorResponse>, GetDepartmentAncestorsCommand> handler,
        [FromRoute] Guid departmentId,
        CancellationToken cancellationToken)
    {
        var command = new GetDepartmentAncestorsCommand(departmentId);
        return await handler.ExecuteAsync(command, cancellationToken).ConfigureAwait(false);
    }

    [HttpGet("tree")]
    [ProducesResponseType<Envelope<List<DepartmentTreeItemResponse>>>(200)]
    [ProducesResponseType<Envelope>(500)]
    public async Task<EndPointResult<List<DepartmentTreeItemResponse>>> GetTreeAsync(
        [FromServices] ICommandHandler<List<DepartmentTreeItemResponse>, GetDepartmentTreeCommand> handler,
        CancellationToken cancellationToken)
    {
        return await handler.ExecuteAsync(new GetDepartmentTreeCommand(), cancellationToken)
            .ConfigureAwait(false);
    }

    [HttpGet("tree/search")]
    [ProducesResponseType<Envelope<List<DepartmentTreeSearchItemResponse>>>(200)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(400)]
    public async Task<EndPointResult<List<DepartmentTreeSearchItemResponse>>> SearchTreeAsync(
        [FromServices] ICommandHandler<List<DepartmentTreeSearchItemResponse>, SearchDepartmentTreeCommand> handler,
        [FromQuery] string? q,
        CancellationToken cancellationToken)
    {
        var command = new SearchDepartmentTreeCommand(q ?? string.Empty);
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

    [HttpPut("{departmentId:guid}/parent")]
    [ProducesResponseType<Envelope<ChangeDepartmentParentResponse>>(200)]
    [ProducesResponseType<Envelope>(400)]
    [ProducesResponseType<Envelope>(404)]
    [ProducesResponseType<Envelope>(409)]
    [ProducesResponseType<Envelope>(500)]
    public async Task<EndPointResult<ChangeDepartmentParentResponse>> ChangeParentAsync(
        [FromServices] ICommandHandler<ChangeDepartmentParentResponse, ChangeDepartmentParentCommand> handler,
        [FromRoute] Guid departmentId,
        [FromBody] ChangeDepartmentParentDto? request,
        CancellationToken cancellationToken)
    {
        var command = new ChangeDepartmentParentCommand(departmentId, request?.ParentId);
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
