using CSharpFunctionalExtensions;
using Dapper;
using DirectoryService.Contracts;
using DirectoryService.Core.Locations;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.shared;
using DirectoryService.Infrastructure.PostgreSQL.Database;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System.Xml.Linq;

namespace DirectoryService.Infrastructure.PostgreSQL.Repositories;
internal class LocationsRepositoryDapper : ILocationsRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger _logger;
    public LocationsRepositoryDapper(IDbConnectionFactory connectionFactory, ILogger<LocationsRepositoryDapper> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
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

        try
        {
            int v = await connection.ExecuteAsync(request, locationInsertParams).ConfigureAwait(false);

            if (v == 1)
                return location.Id.Value;
            else
                return GeneralErrors.Failure("Ошибка вставки локации");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fail to insert Location");
            return GeneralErrors.Failure("Ошибка вставки локации");
        }
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
        try
        {
            int v = await connection.ExecuteAsync(request, locationDeleteParams).ConfigureAwait(false);
            if (v == 1)
                return true;
            else
                return GeneralErrors.Failure("Ошибка удаления локации");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fail to delete Location");
            return GeneralErrors.Failure("Ошибка удаления локации");
        }
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
            switch (keySearch.Key.ToLower().Trim())
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
        if (request.OrderBy != null && !request.OrderBy.Equals(string.Empty))
        {
            string[] orderByParams = request.OrderBy.Split(' ');
            string orderBy = "";
            switch (orderByParams[0].ToLower())
            {
                case "name":
                    orderBy = "name";
                    break;
                case "address":
                    orderBy = "address";
                    break;
            }
            if(orderBy.Length > 0 && orderByParams.Length == 2)
            {
                switch (orderByParams[1].ToLower())
                {
                    case "asc":
                        orderBy += " ASC";
                        break;
                    case "desc":
                        orderBy += " DESC";
                        break;
                }
            }
            if (orderBy.Length > 0)
                requestSql += $" ORDER BY {orderBy}";
        }            
        else
            requestSql += " ORDER BY id";

        requestSql += " OFFSET @skip LIMIT @take";

        var locationSelectParams = new
        {
            name = keyName.Trim(),
            address = keyAddress.Trim(),
            skip = (request.Page - 1) * request.PageSize,
            take = request.PageSize,
        };
        try
        {
            var resultGetByIdAsync = await connection.QueryAsync<Location>(requestSql, locationSelectParams).ConfigureAwait(false);
            return resultGetByIdAsync.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fail to select Location");
            return GeneralErrors.Failure("Ошибка выбора локации");
        }
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

        try
        {
            var resultGetByIdAsync = await connection.QueryFirstOrDefaultAsync<Location?>(request, locationSelectByIdParams).ConfigureAwait(false);
            return resultGetByIdAsync;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fail to select Location");
            return GeneralErrors.Failure("Ошибка выбора локации");
        }
    }
    public async Task<Result<List<Location>, Error>> GetByIdsAsync(IEnumerable<Guid> locationIds, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync().ConfigureAwait(false);
        string request =
            """
            SELECT id, name, address FROM locations WHERE id = ANY(@ids)
            """;
        var locationSelectByIdsParams = new
        {
            ids = locationIds
        };

        try
        {
            return (await connection.QueryAsync<Location>(request, locationSelectByIdsParams)
                .ConfigureAwait(false))
                .ToList<Location>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fail to select Locations");
            return GeneralErrors.Failure("Ошибка выбора локаций");
        }
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

        try
        {
            int v = await connection.ExecuteScalarAsync<int>(request, locationDeleteParams).ConfigureAwait(false);
            if (v > 0)
                return true;
            else
                return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fail to select Location");
            return GeneralErrors.Failure("Ошибка выбора локации");
        }
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

        try
        {
            int v = await connection.ExecuteAsync(request, locationInsertParams).ConfigureAwait(false);
            if (v == 1)
                return true;
            else
                return GeneralErrors.Failure("Ошибка обновления локации");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fail to update Location");
            return GeneralErrors.Failure("Ошибка обновления локации");
        }
    }
}
