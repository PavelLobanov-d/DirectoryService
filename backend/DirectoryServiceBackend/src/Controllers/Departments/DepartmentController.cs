using CSharpFunctionalExtensions;
using DirectoryService.Domain.Departments;
using DirectoryService.Contracts;
using DirectoryService.Contracts.Departments;
using DirectoryService.Core.Departments;
using DirectoryService.Domain.shared;
using DirectoryService.Domain.shared.Exceptions;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace DirectoryService.Controllers.Departments;


[ApiController]
[Route("[controller]")]
public class DepartmentController : ControllerBase
{
    private readonly IDepartmentsService _departmentService;
    public DepartmentController(IDepartmentsService departmentService)
    {
        _departmentService = departmentService;
    }

    [HttpPost]
    [ProducesResponseType<Envelope<CreatedObjectGuid>>(201)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(400)]
    [ProducesResponseType<Envelope>(404)]
    [ProducesResponseType<Envelope>(409)]
    public async Task<EndPointResult<CreatedObjectGuid>> CreateAsync(
        [FromBody] CreateDepartmentDto request,
        CancellationToken cancellationToken)
    {
        return await _departmentService.CreateAsync(request, cancellationToken).ConfigureAwait(false);
    }
    [HttpGet]
    [ProducesResponseType<List<Department>> (200)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(404)]
    public async Task<EndPointResult<List<Department>>> GetAsync(
        [FromQuery] SelectDto request,
        CancellationToken cancellationToken)
    {
        return await _departmentService.GetAsync(request, cancellationToken).ConfigureAwait(false);
    }

    [HttpPatch("{departmentId:guid}/locations/{locationId:guid}")]
    [ProducesResponseType<Envelope<CreatedObjectGuid>>(201)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(400)]
    [ProducesResponseType<Envelope>(404)]
    [ProducesResponseType<Envelope>(409)]
    public async Task<EndPointResult<CreatedObjectGuid>> LinkLocationAsync(
        [FromRoute] Guid departmentId,
        [FromRoute] Guid locationId,
        CancellationToken cancellationToken)
    {
        return await _departmentService.LinkLocationAsync(departmentId, locationId, cancellationToken).ConfigureAwait(false);
    }

    [HttpDelete("{departmentId:guid}/locations/{locationId:guid}")]
    [ProducesResponseType<Envelope<List<Department>>>(200)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(404)]
    public async Task<EndPointResult<bool>> DetachLocationAsync(
        [FromRoute] Guid departmentId,
        [FromRoute] Guid locationId,
        CancellationToken cancellationToken)
    {
        return await _departmentService.DetachLocationAsync(departmentId, locationId, cancellationToken).ConfigureAwait(false);
    }

    [HttpGet("{departmentId:guid}")]
    [ProducesResponseType<Envelope<List<Department>>>(200)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(404)]
    public async Task<EndPointResult<Department?>> GetByIdAsync(
        [FromRoute] Guid departmentId,
        CancellationToken cancellationToken)
    {
        return await _departmentService.GetByIdAsync(departmentId, cancellationToken).ConfigureAwait(false);
    }

    [HttpPut("{departmentId:guid}")]
    [ProducesResponseType<Envelope<List<Department>>>(200)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(400)]
    [ProducesResponseType<Envelope>(404)]
    [ProducesResponseType<Envelope>(409)]
    public async Task<EndPointResult<bool>> UpdateAsync(
        [FromRoute] Guid departmentId,
        [FromBody] UpdateDepartmentDto request,
        CancellationToken cancellationToken)
    {
        return await _departmentService.UpdateAsync(departmentId, request, cancellationToken).ConfigureAwait(false);
    }

    [HttpDelete("{departmentId:guid}")]
    [ProducesResponseType<Envelope<List<Department>>>(200)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(404)]
    public async Task<EndPointResult<bool>> DeleteAsync(
        [FromRoute] Guid departmentId,
        CancellationToken cancellationToken)
    {
        return await _departmentService.DeleteAsync(departmentId, cancellationToken).ConfigureAwait(false);
    }

    [HttpPut("move/{departmentId:guid}")]
    [ProducesResponseType<Envelope<List<Department>>>(200)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(404)]
    [ProducesResponseType<Envelope>(409)]
    public async Task<EndPointResult<bool>> MoveAsync(
    [FromRoute] Guid departmentId,
    [FromBody] Guid newDepartmentId,
    CancellationToken cancellationToken)
    {
        return await _departmentService.ChangeParentAsync(departmentId, newDepartmentId, cancellationToken).ConfigureAwait(false);
    }

    [HttpPatch("{departmentId:guid}/positions/{positionMatrixId:guid}")]
    [ProducesResponseType<Envelope<CreatedObjectGuid>>(201)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(404)]
    [ProducesResponseType<Envelope>(409)]
    public async Task<EndPointResult<CreatedObjectGuid>> LinkPosition(
        [FromRoute] Guid departmentId,
        [FromRoute] Guid positionMatrixId,
        CancellationToken cancellationToken)
    {
        return await _departmentService.LinkPositionAsync(departmentId, positionMatrixId, cancellationToken).ConfigureAwait(false);
    }

    [HttpPatch("detachposition/{departmentPositionId:guid}")]
    [ProducesResponseType<Envelope<List<Department>>>(200)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(404)]
    public async Task<EndPointResult<bool>> DetachPosition(
    [FromRoute] Guid departmentPositionId,
    CancellationToken cancellationToken)
    {
        return await _departmentService.DetachPositionAsync(departmentPositionId, cancellationToken).ConfigureAwait(false);
    }
}
