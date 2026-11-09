using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Locations.Delete;

public record LocationDeleteCommand(Guid LocationId) : ICommand;
