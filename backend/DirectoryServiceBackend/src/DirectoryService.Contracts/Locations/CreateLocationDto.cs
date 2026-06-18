namespace DirectoryService.Contracts.Locations;

/// <summary>
/// запрос на создание локации
/// </summary>
/// <param name="Name"></param>
/// <param name="Address"></param>
public record CreateLocationDto(string Name, string Address);
