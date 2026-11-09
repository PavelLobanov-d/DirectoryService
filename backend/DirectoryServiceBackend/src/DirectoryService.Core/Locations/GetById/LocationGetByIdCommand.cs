using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Locations.GetById;

public record LocationGetByIdCommand(Guid LocationId) : ICommand;
