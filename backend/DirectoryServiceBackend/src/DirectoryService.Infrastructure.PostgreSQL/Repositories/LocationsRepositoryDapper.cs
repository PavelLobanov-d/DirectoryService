using CSharpFunctionalExtensions;
using Dapper;
using DirectoryService.Contracts;
using DirectoryService.Core.Locations;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.shared;
using DirectoryService.Infrastructure.PostgreSQL.Database;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;

namespace DirectoryService.Infrastructure.PostgreSQL.Repositories;
internal class LocationsRepositoryDapper : ILocationsRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    public LocationsRepositoryDapper(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }
    public async Task<Result<Guid, Error>> AddAsync(Location location, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync().ConfigureAwait(false);
        const string request = 
            """
            INSERT INTO locations (id, name, address)
            VALUES(@id, @locationName, @locationAddress)
            """;
        var locationInsertParams = new
        {
            id = location.Id.Value,
            locationName = location.Name.Value,
            locationAddress = location.Address.Value
        };

        int v = await connection.ExecuteAsync(request, locationInsertParams).ConfigureAwait(false);
        if (v == 1)
            return location.Id.Value;
        else
            return GeneralErrors.Failure("Ошибка вставки локации");
    }
    public async Task<Result<bool, Error>> DeleteAsync(Guid locationId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync().ConfigureAwait(false);
        const string request =
            """
            DELETE FROM locations WHERE id = @id
            """;
        var locationDeleteParams = new
        {
            id = locationId
        };

        int v = await connection.ExecuteAsync(request, locationDeleteParams).ConfigureAwait(false);
        if (v == 1)
            return true;
        else
            return GeneralErrors.Failure("Ошибка удаления локации");
    }
    public Task<Result<bool, Error>> DeleteAsync(Location location, CancellationToken cancellationToken = default)
    {
        return DeleteAsync(location.Id.Value, cancellationToken);
    }
    public async Task<Result<List<Location>, Error>> GetAsync(SelectDto request, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync().ConfigureAwait(false);

        string requestSql =
            """
            SELECT id, name, address FROM locations
            """;

        int paramCount = 0;
        string keyName = "";
        string keyAddress = "";
        Dictionary<string, StringValues> parsedQuery = QueryHelpers.ParseQuery(request.Search);

        foreach (KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> keySearch in parsedQuery)
        {
            switch (keySearch.Key.ToLower())
            {
                case "name":
                    if (paramCount == 0)
                        requestSql += " WHERE ";
                    else
                        requestSql += " AND ";
                    paramCount++;
                    requestSql += "name = @name";
                    keyName = keySearch.Value;
                    break;
                case "address":
                    if (paramCount == 0)
                        requestSql += " WHERE ";
                    else
                        requestSql += " AND ";
                    paramCount++;
                    requestSql += "address = @address";
                    keyAddress = keySearch.Value;
                    break;
            }
        }
        var locationSelectParams = new
        {
            name = keyName,
            address = keyAddress
        };


        var resultGetByIdAsync = await connection.QueryAsync<Location>(requestSql, locationSelectParams).ConfigureAwait(false);
        return resultGetByIdAsync.ToList();
    }
    public async Task<Result<Location?, Error>> GetByIdAsync(Guid locationId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync().ConfigureAwait(false);
        string request =
            """
            SELECT id, name, address FROM locations WHERE id = @id
            """;
        var locationSelectByIdParams = new
        {
            id = locationId            
        };

        var resultGetByIdAsync = await connection.QueryFirstOrDefaultAsync<Location?>(request, locationSelectByIdParams).ConfigureAwait(false);
        return resultGetByIdAsync;
    }
    public async Task<Result<bool, Error>> HasNameAsync(string name, Guid? excludeId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync().ConfigureAwait(false);
        string request = 
            """
            SELECT Count(*) FROM locations WHERE name = @name
            """;
        if (excludeId != null)
            request += " AND id <> @excludeId";
        var locationDeleteParams = new
        {
            name = name,
            excludeId = excludeId
        };

        int v = await connection.ExecuteScalarAsync<int>(request, locationDeleteParams).ConfigureAwait(false);
        if (v > 0)
            return true;
        else
            return false;
    }
    public async Task<Result<bool, Error>> SaveAsync(CancellationToken cancellationToken = default)
    {
        return true;
    }
    public async Task<Result<bool, Error>> UpdateAsync(Location location, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync().ConfigureAwait(false);

        const string request =
            """
            UPDATE locations SET name = @locationName, address = @locationAddress
            WHERE id = @id
            """;
        var locationInsertParams = new
        {
            id = location.Id.Value,
            locationName = location.Name.Value,
            locationAddress = location.Address.Value
        };

        int v = await connection.ExecuteAsync(request, locationInsertParams).ConfigureAwait(false);
        if (v == 1)
            return true;
        else
            return GeneralErrors.Failure("Ошибка обновления локации");
    }
}
