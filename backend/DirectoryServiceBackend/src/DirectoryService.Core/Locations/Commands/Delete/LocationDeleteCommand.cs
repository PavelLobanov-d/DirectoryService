using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Locations.Commands.Delete;

public record LocationDeleteCommand(Guid LocationId) : ICommand;
