namespace DirectoryService.Contracts.Locations;

/// <summary>
/// запрос на изменение локации
/// </summary>
/// <param name="NewName">имя (null, если не изменяется)</param>
/// <param name="NewAddress">адрес (null, если не изменяется)</param>
public record UpdateLocationDto(string? NewName, string? NewAddress);

