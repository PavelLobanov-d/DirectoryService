using CSharpFunctionalExtensions;
using DirectoryService.Domain.shared;
using System.Text.RegularExpressions;
//using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DirectoryService.Domain.Departments;

public partial record DepartmentName
{
    public const int NAME_MIN_LENGHT = 3;
    public const int NAME_MAX_LENGHT = 150;

    public string Value { get; } = string.Empty;

    private DepartmentName() { }
    private DepartmentName(string value)
    {
        Value = value;
    }

    public static Result<DepartmentName, Error> Create(string value)
    {
        string normalized = _manySpaces.Replace(input: value.Trim(), " ");

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return GeneralErrors.ValueIsRequired("название");
        }

        if (normalized.Length is < NAME_MIN_LENGHT or > NAME_MAX_LENGHT)
        {
            return GeneralErrors.ValueIsInvalid("название");
        }

        return new DepartmentName(normalized);
    }

    [GeneratedRegex("\\s+", options: RegexOptions.Compiled)]
    private static partial Regex _manySpaces { get; }
    public override string ToString() => Value.ToString();
}
