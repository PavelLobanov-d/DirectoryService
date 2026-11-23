
namespace DirectoryService.Core.Locations.Queries.GetById;

public record LocationGetByIdCommand(Guid LocationId) : ICommand;
