using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Locations.Queries.GetByIds;

public record LocationGetByIdsCommand(IEnumerable<Guid> LocationIds) : ICommand;
