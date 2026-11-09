using DirectoryService.Contracts;
using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Locations.Get;

public record LocationGetCommand(SelectDto Request) : ICommand;
