using DirectoryService.Contracts.Locations;

namespace DirectoryService.Core.Locations.Commands.Update;

public record LocationUpdateCommand(Guid LocationId, UpdateLocationDto LocationDto) : ICommand;
