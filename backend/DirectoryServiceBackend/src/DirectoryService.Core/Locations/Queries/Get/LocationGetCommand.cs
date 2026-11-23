using DirectoryService.Contracts.Locations;

namespace DirectoryService.Core.Locations.Queries.Get;

public record LocationGetCommand(GetLocationsRequest Request) : ICommand;
