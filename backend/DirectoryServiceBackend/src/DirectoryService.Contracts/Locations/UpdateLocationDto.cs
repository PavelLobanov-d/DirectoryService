namespace DirectoryService.Contracts.Locations;

/// <summary>
/// запрос на изменение локации
/// </summary>
/// <param name="locationId">Id</param>
/// <param name="NewName">имя (null, если не изменяется)</param>
/// <param name="NewAddress">адрес (null, если не изменяется)</param>
public record UpdateLocationDto(Guid locationId, string? NewName, string? NewAddress);

