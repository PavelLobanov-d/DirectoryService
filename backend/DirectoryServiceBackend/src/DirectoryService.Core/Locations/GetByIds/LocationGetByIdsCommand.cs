using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Locations.GetByIds;

public record LocationGetByIdsCommand(IEnumerable<Guid> LocationIds) : ICommand;
