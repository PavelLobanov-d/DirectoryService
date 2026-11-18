namespace DirectoryService.Core.Departments;

/// <summary>
/// Описание вычисляемого ltree-столбца полного пути подразделения.
/// </summary>
public static class DepartmentPathIndex
{
    public const string ColumnName = "pathslugfull";

    public const string ShadowPropertyName = "PathSlugFullLTree";

    public const string IndexName = "IX_departments_pathslugfull";

    public const string ComputedSql = "(COALESCE(pathslug::text || '.', '') || slug::text)::ltree";
}
