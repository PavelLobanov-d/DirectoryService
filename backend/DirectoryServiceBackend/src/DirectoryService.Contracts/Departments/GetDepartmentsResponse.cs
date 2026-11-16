namespace DirectoryService.Contracts.Departments;

public record GetDepartmentsResponse(
    List<GetDepartmentResponse> Items,
    long TotalCount);
