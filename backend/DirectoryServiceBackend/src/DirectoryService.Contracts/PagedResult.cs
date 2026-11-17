namespace DirectoryService.Contracts;

public record PagedResult<T>(
    List<T> Items,
    long TotalCount,
    int Page,
    int PageSize);
