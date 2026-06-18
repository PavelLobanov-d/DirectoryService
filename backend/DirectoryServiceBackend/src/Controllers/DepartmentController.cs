using Microsoft.AspNetCore.Mvc;
using DirectoryService.Contracts.Departments;

namespace DirectoryService.Controller;


[ApiController]
[Route("[controller]")]
public class DepartmentController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateDepartmentDto request,
        CancellationToken cancellationToken)
    {        
        return Ok($"Department.Create, request.Name={request.Name}");
    }
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] GetDepartmentsDto request,
        CancellationToken cancellationToken)
    {
        return Ok("Department.Get");
    }
    [HttpGet("{departmentId:guid}")]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid requestId,
        CancellationToken cancellationToken)
    {
        return Ok("Department.GetById");
    }
    [HttpPut("{departmentId:guid}")]
    public async Task<IActionResult> Update(
        [FromRoute] Guid requestId,
        [FromBody] UpdateDepartmentDto request,
        CancellationToken cancellationToken)
    {
        return Ok("Department.Update");
    }
    [HttpDelete("{departmentId:guid}")]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid requestId,
        CancellationToken cancellationToken)
    {
        return Ok("Department.Delete");
    }
}
