using DirectoryService.Contracts.Locations;
using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Locations.Update;

public record LocationUpdateCommand(Guid LocationId, UpdateLocationDto LocationDto) : ICommand;
