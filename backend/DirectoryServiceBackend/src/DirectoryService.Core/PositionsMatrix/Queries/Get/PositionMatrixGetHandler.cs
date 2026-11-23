using CSharpFunctionalExtensions;
using DirectoryService.Contracts.PositionsMatrix;
using DirectoryService.Core.Database;
using DirectoryService.Domain.PositionsMatrix;
using DirectoryService.Domain.shared;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace DirectoryService.Core.PositionsMatrix.Queries.Get;

public class PositionMatrixGetHandler : ICommandHandler<List<GetPositionMatrixResponse>, PositionMatrixGetCommand>
{
    private readonly IReadDbContext _readDbContext;
    private readonly ILogger<PositionMatrixGetHandler> _logger;

    public PositionMatrixGetHandler(
        IReadDbContext readDbContext,
        ILogger<PositionMatrixGetHandler> logger)
    {
        _readDbContext = readDbContext;
        _logger = logger;
    }

    public async Task<Result<List<GetPositionMatrixResponse>, Errors>> ExecuteAsync(
        PositionMatrixGetCommand command,
        CancellationToken cancellationToken = default)
    {
        Dictionary<string, StringValues> parsedQuery = QueryHelpers.ParseQuery(command.Request.Search);
        IQueryable<PositionMatrix> query = _readDbContext.PositionsRead;

        foreach (KeyValuePair<string, StringValues> keySearch in parsedQuery)
        {
            if (keySearch.Key.Equals(nameof(PositionMatrix.Name), StringComparison.OrdinalIgnoreCase))
            {
                var resultName = PositionName.Create(keySearch.Value.ToString());
                if (resultName.IsFailure)
                    return resultName.Error.ToErrors();

                query = query.Where(p => p.Name == resultName.Value);
            }
            else if (keySearch.Key.Equals(nameof(PositionMatrix.Slug), StringComparison.OrdinalIgnoreCase))
            {
                var resultSlug = Slug.Create(keySearch.Value.ToString());
                if (resultSlug.IsFailure)
                    return resultSlug.Error.ToErrors();

                query = query.Where(p => p.Slug == resultSlug.Value);
            }
        }

        if (!string.IsNullOrWhiteSpace(command.Request.OrderBy))
        {
            string[] param = command.Request.OrderBy.Split(' ');
            string field = param[0];
            string orderType = param.Length > 1 ? param[1] : string.Empty;

            bool descending = orderType.StartsWith("desc", StringComparison.OrdinalIgnoreCase);
            switch (field.ToLowerInvariant())
            {
                case "name":
                    query = descending
                        ? query.OrderByDescending(p => p.Name)
                        : query.OrderBy(p => p.Name);
                    break;
                case "slug":
                    query = descending
                        ? query.OrderByDescending(p => p.Slug)
                        : query.OrderBy(p => p.Slug);
                    break;
                default:
                    query = query.OrderBy(p => p.Id);
                    break;
            }
        }
        else
        {
            query = query.OrderBy(p => p.Id);
        }

        if (command.Request.Page != null && command.Request.PageSize != null)
        {
            int skipRecords = ((int)command.Request.Page - 1) * (int)command.Request.PageSize;
            query = query
                .Skip(skipRecords)
                .Take((int)command.Request.PageSize);
        }

        var rows = await query
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Path,
                p.ParentId,
                ChildsIds = p.Childs.Select(c => c.Id).ToList(),
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows
            .Select(row => new GetPositionMatrixResponse(
                row.Id.Value,
                row.Name.Value,
                row.Path.Value,
                row.ParentId?.Value,
                row.ChildsIds.Select(c => c.Value).ToList()))
            .ToList();
    }
}
