using DirectoryService.Contracts.Departments;

namespace DirectoryService.Core.Departments.Queries.Tree;

internal static class DepartmentTreeSql
{
    public const string ItemColumns = """
        d.id,
        d.name,
        d.path::text AS path,
        nlevel(d.path) - 1 AS depth,
        EXISTS (
            SELECT 1
            FROM departments child
            WHERE child.parentid = d.id
              AND child.deleted_at IS NULL
        ) AS haschildren,
        (
            SELECT count(*)::int
            FROM departments child
            WHERE child.parentid = d.id
              AND child.deleted_at IS NULL
        ) AS childrencount
        """;

    public const string AncestorColumns = """
        a.id,
        a.name,
        a.path::text AS path,
        nlevel(a.path) - 1 AS depth
        """;
}

internal sealed class DepartmentTreeRow
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Path { get; set; } = null!;
    public int Depth { get; set; }
    public bool HasChildren { get; set; }
    public int ChildrenCount { get; set; }

    public DepartmentTreeItemResponse ToItemResponse() => new(
        Id,
        Name,
        Path,
        Depth,
        HasChildren,
        ChildrenCount);
}

internal sealed class DepartmentAncestorRow
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Path { get; set; } = null!;
    public int Depth { get; set; }

    public DepartmentAncestorResponse ToResponse() => new(
        Id,
        Name,
        Path,
        Depth);
}
