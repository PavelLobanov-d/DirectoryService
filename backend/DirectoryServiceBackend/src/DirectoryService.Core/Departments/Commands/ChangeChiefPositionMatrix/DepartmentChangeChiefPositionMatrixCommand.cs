using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Departments.Commands.ChangeChiefPositionMatrix;

public record DepartmentChangeChiefPositionMatrixCommand(Guid DepartmentId, Guid NewChiefPositionMatrixId) : ICommand;
