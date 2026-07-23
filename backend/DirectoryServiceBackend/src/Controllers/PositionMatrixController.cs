using DirectoryService.Contracts;
using DirectoryService.Contracts.PositionsMatrix;
using DirectoryService.Core.PositionsMatrix;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.shared;
using Microsoft.AspNetCore.Mvc;
using System.Reflection.Metadata;

namespace DirectoryService.Controller;


[ApiController]
[Route("[controller]")]
public class PositionMatrixController : ControllerBase
{
    private readonly IPositionMatrixService _positionMatrixService;
    public PositionMatrixController(IPositionMatrixService positionMatrixService)
    {
        _positionMatrixService = positionMatrixService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreatePositionMatrixDto request,
        CancellationToken cancellationToken)
    {
        var result = await _positionMatrixService.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }
        await _positionMatrixService.SaveAsync(cancellationToken);
        return Ok(result.Value);
    }
    [HttpGet]
    public async Task<IActionResult> GetAsync(
        [FromQuery] SelectDto request,
        CancellationToken cancellationToken)
    {
        var result = await _positionMatrixService.GetAsync(request, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }
        if (result.Value == null)
        {
            return NotFound();
        }
        return Ok($"PositionsMatrix.Get : {result.Value.Count}");
    }
    [HttpGet("{positionMatrixId:guid}")]
    public async Task<IActionResult> GetByIdAsync(
        [FromRoute] Guid positionMatrixId,
        CancellationToken cancellationToken)
    {
        var result = await _positionMatrixService.GetByIdAsync(positionMatrixId, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }
        if(result.Value == null)
        {
            return NotFound();
        }
        return Ok($"PositionMatrix.GetByIdAsync : {result.Value.Name}");
    }
    [HttpPut("{positionMatrixId:guid}")]
    public async Task<IActionResult> UpdateAsync(
        [FromRoute] Guid positionMatrixId,
        [FromBody] UpdatePositionMatrixDto request,
        CancellationToken cancellationToken)
    {
        var result = await _positionMatrixService.UpdateAsync(positionMatrixId, request, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }
        await _positionMatrixService.SaveAsync(cancellationToken);

        return Ok($"PositionMatrix.UpdateAsync : {result.Value}");
    }
    [HttpPut("move/{positionMatrixId:guid}")]
    public async Task<IActionResult> Move(
        [FromRoute] Guid positionMatrixId,
        [FromBody] Guid newParentId,
        CancellationToken cancellationToken)
    {
        var result = await _positionMatrixService.ChangeParentAsync(positionMatrixId, newParentId, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }
        await _positionMatrixService.SaveAsync(cancellationToken);

        return Ok($"PositionMatrix.ChangeParentAsync : {result.Value}");
    }

    [HttpDelete("{positionMatrixId:guid}")]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid positionMatrixId,
        CancellationToken cancellationToken)
    {
        var result = await _positionMatrixService.DeleteAsync(positionMatrixId, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }
        await _positionMatrixService.SaveAsync(cancellationToken);

        return Ok($"PositionMatrix.Delete : {result.Value}");
    }
}
