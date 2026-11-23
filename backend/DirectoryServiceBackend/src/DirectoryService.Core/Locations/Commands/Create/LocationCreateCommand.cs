using DirectoryService.Contracts.Locations;

namespace DirectoryService.Core.Locations.Commands.Create;

public record LocationCreateCommand(CreateLocationDto LocationDto) : ICommand;
