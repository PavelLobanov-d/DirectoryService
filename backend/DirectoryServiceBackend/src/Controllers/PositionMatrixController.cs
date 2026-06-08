using System.Reflection.Metadata;
using Microsoft.AspNetCore.Mvc;
using DirectoryService.Contracts.PositionsMatrix;

namespace DirectoryService.Controller;


[ApiController]
[Route("[controller]")]
public class PositionMatrixController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreatePositionMatrixDto request,
        CancellationToken cancellationToken)
    {
        return Ok("PositionMatrix.Create");
    }
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] GetPositionsMatrixDto request,
        CancellationToken cancellationToken)
    {
        return Ok("PositionMatrix.Get");
    }
    [HttpGet("{positionMatrixId:guid}")]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid requestId,
        CancellationToken cancellationToken)
    {
        return Ok("PositionMatrix.GetById");
    }
    [HttpPut("{positionMatrixId:guid}")]
    public async Task<IActionResult> Update(
        [FromRoute] Guid requestId,
        [FromBody] UpdatePositionMatrixDto request,
        CancellationToken cancellationToken)
    {
        return Ok("PositionMatrix.Update");
    }
    [HttpDelete("{positionMatrixId:guid}")]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid requestId,
        CancellationToken cancellationToken)
    {
        return Ok("PositionMatrix.Delete");
    }
}
