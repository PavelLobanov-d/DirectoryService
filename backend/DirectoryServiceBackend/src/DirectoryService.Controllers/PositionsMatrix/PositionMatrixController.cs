using DirectoryService.Contracts;
using DirectoryService.Contracts.PositionsMatrix;
using DirectoryService.Core.PositionsMatrix;
using DirectoryService.Domain.PositionsMatrix;
using DirectoryService.Shared;
using Microsoft.AspNetCore.Mvc;
using System.Reflection.Metadata;

namespace DirectoryService.Controllers.PositionsMatrix;


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
    [ProducesResponseType<Envelope<CreatedObjectGuid>>(201)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(404)]
    [ProducesResponseType<Envelope>(409)]
    public async Task<EndPointResult<CreatedObjectGuid>> CreateAsync(
        [FromBody] CreatePositionMatrixDto request,
        CancellationToken cancellationToken)
    {
        return await _positionMatrixService.CreateAsync(request, cancellationToken).ConfigureAwait(false);
    }

    [HttpGet]
    [ProducesResponseType<Envelope<List<PositionMatrix>>>(200)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(404)]
    public async Task<EndPointResult<List<PositionMatrix>>> GetAsync(
        [FromQuery] SelectDto request,
        CancellationToken cancellationToken)
    {
        return await _positionMatrixService.GetAsync(request, cancellationToken).ConfigureAwait(false);
    }

    [HttpGet("{positionMatrixId:guid}")]
    [ProducesResponseType<Envelope<PositionMatrix>>(200)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(404)]
    public async Task<EndPointResult<PositionMatrix?>> GetByIdAsync(
        [FromRoute] Guid positionMatrixId,
        CancellationToken cancellationToken)
    {
        return await _positionMatrixService.GetByIdAsync(positionMatrixId, cancellationToken).ConfigureAwait(false);
    }

    [HttpPut("{positionMatrixId:guid}")]
    [ProducesResponseType<Envelope<bool>>(200)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(404)]
    [ProducesResponseType<Envelope>(409)]
    public async Task<EndPointResult<bool>> UpdateAsync(
        [FromRoute] Guid positionMatrixId,
        [FromBody] UpdatePositionMatrixDto request,
        CancellationToken cancellationToken)
    {
        return await _positionMatrixService.UpdateAsync(positionMatrixId, request, cancellationToken).ConfigureAwait(false);
    }

    [HttpPut("move/{positionMatrixId:guid}")]
    [ProducesResponseType<Envelope<bool>>(200)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(404)]
    [ProducesResponseType<Envelope>(409)]
    public async Task<EndPointResult<bool>> Move(
        [FromRoute] Guid positionMatrixId,
        [FromBody] Guid newParentId,
        CancellationToken cancellationToken)
    {
        return await _positionMatrixService.ChangeParentAsync(positionMatrixId, newParentId, cancellationToken).ConfigureAwait(false);
    }

    [HttpDelete("{positionMatrixId:guid}")]
    [ProducesResponseType<Envelope<bool>>(200)]
    [ProducesResponseType<Envelope>(500)]
    [ProducesResponseType<Envelope>(404)]
    public async Task<EndPointResult<bool>> Delete(
        [FromRoute] Guid positionMatrixId,
        CancellationToken cancellationToken)
    {
        return await _positionMatrixService.DeleteAsync(positionMatrixId, cancellationToken).ConfigureAwait(false);
    }
}
