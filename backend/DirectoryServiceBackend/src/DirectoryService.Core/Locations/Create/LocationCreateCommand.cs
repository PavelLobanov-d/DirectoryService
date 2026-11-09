using DirectoryService.Contracts.Locations;
using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Locations.Create;

public record LocationCreateCommand(CreateLocationDto LocationDto) : ICommand;
