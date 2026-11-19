using System.Text.RegularExpressions;

namespace DirectoryService.Core.Departments;

/// <summary>
/// Общие правила поиска подразделений.
/// </summary>
public static partial class DepartmentSearch
{
    public static bool IsValidPath(string path) => PathRegex().IsMatch(path);

    public static string EscapeLike(string value) =>
        value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);

    [GeneratedRegex("^[a-z0-9]+(\\.[a-z0-9]+)*$", options: RegexOptions.NonBacktracking)]
    private static partial Regex PathRegex();
}
