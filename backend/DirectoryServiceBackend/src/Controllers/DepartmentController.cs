using DirectoryService.Contracts;
using DirectoryService.Contracts.Departments;
using DirectoryService.Core.DepartmentChiefPositions;
using DirectoryService.Core.DepartmentPositions;
using DirectoryService.Core.Departments;
using DirectoryService.Core.PositionsMatrix;
using DirectoryService.Domain.PositionsMatrix;
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
            return BadRequest(result.Error);
        }

        var resultSave = await _departmentService.SaveAsync(cancellationToken);
        if (resultSave.IsFailure)
        {
            return BadRequest(resultSave.Error);
        }
        return Ok($"DepartmentService.CreateAsync : {resultSave.Value}");
    }
    [HttpGet]
    public async Task<IActionResult> GetAsync(
        [FromQuery] SelectDto request,
        CancellationToken cancellationToken)
    {
        var result = await _departmentService.GetAsync(request, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }
        if (result.Value == null)
        {
            return NotFound();
        }
        return Ok($"DepartmentService.GetAsync : {result.Value.Count}");
    }
    [HttpGet("{departmentId:guid}")]
    public async Task<IActionResult> GetByIdAsync(
        [FromRoute] Guid departmentId,
        CancellationToken cancellationToken)
    {
        var result = await _departmentService.GetByIdAsync(departmentId, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }
        if (result.Value == null)
        {
            return NotFound();
        }
        return Ok($"DepartmentService.GetByIdAsync : {result.Value.Name}");
    }
    [HttpPut("{departmentId:guid}")]
    public async Task<IActionResult> Update(
        [FromRoute] Guid departmentId,
        [FromBody] UpdateDepartmentDto request,
        CancellationToken cancellationToken)
    {
        var result = await _departmentService.UpdateAsync(departmentId, request, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        var resultSave = await _departmentService.SaveAsync(cancellationToken);
        if (resultSave.IsFailure)
        {
            return BadRequest(resultSave.Error);
        }

        return Ok($"DepartmentService.Update : {resultSave.Value}");
    }

    [HttpDelete("{departmentId:guid}")]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid departmentId,
        CancellationToken cancellationToken)
    {
        var result = await _departmentService.DeleteAsync(departmentId, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        var resultSave = await _departmentService.SaveAsync(cancellationToken);
        if (resultSave.IsFailure)
        {
            return BadRequest(resultSave.Error);
        }

        return Ok($"DepartmentService.Delete : {resultSave.Value}");
    }

    [HttpPut("move/{departmentId:guid}")]
    public async Task<IActionResult> Move(
    [FromRoute] Guid departmentId,
    [FromBody] Guid newDepartmentId,
    CancellationToken cancellationToken)
    {
        var result = await _departmentService.ChangeParentAsync(departmentId, newDepartmentId, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        var resultSave = await _departmentService.SaveAsync(cancellationToken);
        if (resultSave.IsFailure)
        {
            return BadRequest(resultSave.Error);
        }

        return Ok($"DepartmentService.ChangeParentAsync : {resultSave.Value}");
    }

    [HttpPut("linkposition/{departmentId:guid}")]
    public async Task<IActionResult> LinkPosition(
        [FromRoute] Guid departmentId,
        [FromBody] Guid positionMatrixId,
        CancellationToken cancellationToken)
    {
        var result = await _departmentService.LinkPositionAsync(departmentId, positionMatrixId, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        var resultSave = await _departmentService.SaveAsync(cancellationToken);
        if (resultSave.IsFailure)
        {
            return BadRequest(resultSave.Error);
        }
        return Ok($"DepartmentService.LinkPosition : {resultSave.Value}");
    }

    [HttpPut("detachposition/{departmentPositionId:guid}")]
    public async Task<IActionResult> DetachPosition(
    [FromRoute] Guid departmentPositionId,
    CancellationToken cancellationToken)
    {
        var result = await _departmentService.DetachPositionAsync(departmentPositionId, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        var resultSave = await _departmentService.SaveAsync(cancellationToken);
        if(resultSave.IsFailure)
        {
            return BadRequest(resultSave.Error);
        }
        return Ok($"DepartmentService.DetachPosition : {resultSave.Value}");
    }

    [HttpPut("linklocation/{departmentId:guid}")]
    public async Task<IActionResult> LinkLocation(
    [FromRoute] Guid departmentId,
    [FromBody] Guid locationId,
    CancellationToken cancellationToken)
    {
        var result = await _departmentService.LinkLocationAsync(departmentId, locationId, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        var resultSave = await _departmentService.SaveAsync(cancellationToken);
        if (resultSave.IsFailure)
        {
            return BadRequest(resultSave.Error);
        }

        return Ok($"DepartmentService.LinkLocation : {resultSave.Value}");
    }

    [HttpPut("detachlocation/{departmentId:guid}")]
    public async Task<IActionResult> DetachLocation(
    [FromRoute] Guid departmentId,
    [FromBody] Guid locationId,
    CancellationToken cancellationToken)
    {
        var result = await _departmentService.DetachLocationAsync(departmentId, locationId, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        var resultSave = await _departmentService.SaveAsync(cancellationToken);
        if (resultSave.IsFailure)
        {
            return BadRequest(resultSave.Error);
        }

        return Ok($"DepartmentService.DetachLocation : {resultSave.Value}");
    }

}
