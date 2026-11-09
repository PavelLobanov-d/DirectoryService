using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Departments.ChangeChiefPositionMatrix;

public record DepartmentChangeChiefPositionMatrixCommand(Guid DepartmentId, Guid NewChiefPositionMatrixId) : ICommand;
