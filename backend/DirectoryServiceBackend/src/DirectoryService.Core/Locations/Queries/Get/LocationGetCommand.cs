using DirectoryService.Contracts.Locations;
using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Locations.Queries.Get;

public record LocationGetCommand(GetLocationsRequest Request) : ICommand;
