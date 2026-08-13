using CSharpFunctionalExtensions;
using DirectoryService.Contracts;
using DirectoryService.Contracts.Departments;
using DirectoryService.Core.Departments;
using DirectoryService.Domain.shared;
using DirectoryService.Domain.shared.Exceptions;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace DirectoryService.Controller;


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
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateDepartmentDto request,
        CancellationToken cancellationToken)
    {
        var result = await _departmentService.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            //return BadRequest(result.Error);
            throw new BadRequestException(result.Error);
        }

        var resultSave = await _departmentService.SaveAsync(cancellationToken);
        if (resultSave.IsFailure)
        {
            //return BadRequest(resultSave.Error);
            throw new BadRequestException(resultSave.Error);
        }
        return Ok(new { result.Value });
    }
    [HttpGet]
    public async Task<IActionResult> GetAsync(
        [FromQuery] SelectDto request,
        CancellationToken cancellationToken)
    {
        var result = await _departmentService.GetAsync(request, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            //return BadRequest(result.Error);
            throw new BadRequestException(result.Error);
        }
        if (result.Value == null)
        {
            //return NotFound();
            throw new NotFoundException(result.Error);
        }
        return Ok(new { result.Value });
    }
    [HttpPatch("{departmentId:guid}/locations/{locationId:guid}")]
    public async Task<IActionResult> LinkLocationAsync(
        [FromRoute] Guid departmentId,
        [FromRoute] Guid locationId,
        CancellationToken cancellationToken)
    {
        var result = await _departmentService.LinkLocationAsync(departmentId, locationId, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            //return BadRequest(result.Error);
            throw new BadRequestException(result.Error);
        }

        var resultSave = await _departmentService.SaveAsync(cancellationToken);
        if (resultSave.IsFailure)
        {
            //return BadRequest(resultSave.Error);
            throw new BadRequestException(resultSave.Error);
        }

        return Ok(new { result.Value });
    }

    [HttpDelete("{departmentId:guid}/locations/{locationId:guid}")]
    public async Task<IActionResult> DetachLocationAsync(
        [FromRoute] Guid departmentId,
        [FromRoute] Guid locationId,
        CancellationToken cancellationToken)
    {
        var result = await _departmentService.DetachLocationAsync(departmentId, locationId, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            //return BadRequest(result.Error);
            throw new BadRequestException(result.Error);
        }
        if (!result.Value)
        {
            //return BadRequest(result.Error);
            throw new BadRequestException(GeneralErrors.Failure($"Ошибка удаления связки департамент({departmentId})-локация({locationId})"));
        }

        var resultSave = await _departmentService.SaveAsync(cancellationToken);
        if (resultSave.IsFailure)
        {
            //return BadRequest(resultSave.Error);
            throw new BadRequestException(resultSave.Error);
        }

        return Ok(new { result.Value });
    }

    [HttpGet("{departmentId:guid}")]
    public async Task<IActionResult> GetByIdAsync(
        [FromRoute] Guid departmentId,
        CancellationToken cancellationToken)
    {
        var result = await _departmentService.GetByIdAsync(departmentId, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            //return BadRequest(result.Error);
            throw new BadRequestException(result.Error);
        }
        if (result.Value == null)
        {
            //return NotFound();
            throw new NotFoundException(GeneralErrors.NotFound(departmentId, "Department"));
        }
        //return Ok(new { result.Value.Id.Value });
        return Ok(new { result.Value });
    }

    [HttpPut("{departmentId:guid}")]
    public async Task<IActionResult> UpdateAsync(
        [FromRoute] Guid departmentId,
        [FromBody] UpdateDepartmentDto request,
        CancellationToken cancellationToken)
    {
        var result = await _departmentService.UpdateAsync(departmentId, request, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            //return BadRequest(result.Error);
            throw new BadRequestException(result.Error);
        }

        var resultSave = await _departmentService.SaveAsync(cancellationToken);
        if (resultSave.IsFailure)
        {
            //return BadRequest(resultSave.Error);
            throw new BadRequestException(resultSave.Error);
        }

        return Ok(new {result.Value });
    }

    [HttpDelete("{departmentId:guid}")]
    public async Task<IActionResult> DeleteAsync(
        [FromRoute] Guid departmentId,
        CancellationToken cancellationToken)
    {
        var result = await _departmentService.DeleteAsync(departmentId, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            //return BadRequest(result.Error);
            throw new BadRequestException(result.Error);
        }

        var resultSave = await _departmentService.SaveAsync(cancellationToken);
        if (resultSave.IsFailure)
        {
            //return BadRequest(resultSave.Error);
            throw new BadRequestException(resultSave.Error);
        }

        return Ok(new { result.Value });
    }

    [HttpPut("move/{departmentId:guid}")]
    public async Task<IActionResult> MoveAsync(
    [FromRoute] Guid departmentId,
    [FromBody] Guid newDepartmentId,
    CancellationToken cancellationToken)
    {
        var result = await _departmentService.ChangeParentAsync(departmentId, newDepartmentId, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            //return BadRequest(result.Error);
            throw new BadRequestException(result.Error);
        }

        var resultSave = await _departmentService.SaveAsync(cancellationToken);
        if (resultSave.IsFailure)
        {
            //return BadRequest(resultSave.Error);
            throw new BadRequestException(resultSave.Error);
        }

        return Ok(new { result.Value });
    }

    [HttpPatch("{departmentId:guid}/positions/{positionMatrixId:guid}")]
    public async Task<IActionResult> LinkPosition(
        [FromRoute] Guid departmentId,
        [FromRoute] Guid positionMatrixId,
        CancellationToken cancellationToken)
    {
        var result = await _departmentService.LinkPositionAsync(departmentId, positionMatrixId, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            //return BadRequest(result.Error);
            throw new BadRequestException(result.Error);
        }

        var resultSave = await _departmentService.SaveAsync(cancellationToken);
        if (resultSave.IsFailure)
        {
            //return BadRequest(resultSave.Error);
            throw new BadRequestException(resultSave.Error);
        }
        return Ok(new { result.Value });
    }

    [HttpPatch("detachposition/{departmentPositionId:guid}")]
    public async Task<IActionResult> DetachPosition(
    [FromRoute] Guid departmentPositionId,
    CancellationToken cancellationToken)
    {
        var result = await _departmentService.DetachPositionAsync(departmentPositionId, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            //return BadRequest(result.Error);
            throw new BadRequestException(result.Error);
        }

        var resultSave = await _departmentService.SaveAsync(cancellationToken);
        if(resultSave.IsFailure)
        {
            //return BadRequest(resultSave.Error);
            throw new BadRequestException(resultSave.Error);
        }
        return Ok(new { result.Value });
    }
}
