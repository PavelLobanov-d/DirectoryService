using DirectoryService.Domain.Statistics;
using static DirectoryService.Domain.Statistics.Statistica;

namespace DirectoryService.Domain.GlobalStatisticsClass;

public sealed class GlobalStatistics
{
    public GlobalStatistics() { }

    private readonly List<Statistica> _stats = [];
    public IReadOnlyList<Statistica> Stats => _stats;

    public Statistica AddStatistica(Guid objectId,
        string objectTypeName,
        Statistica.Level level,
        Statistica.Action action,
        string description,
        Guid? parentId = null,
        string? parentTypeName = null)
    {
        Statistica newObj = Statistica.Create(objectId,
        objectTypeName,
        level,
        action,
        description,
        parentId,
        parentTypeName);

        _stats.Add(newObj);

        return newObj;
    }
}
