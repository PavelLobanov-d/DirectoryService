using CSharpFunctionalExtensions;
using DirectoryService.Domain.shared;
using System.Text.RegularExpressions;

namespace DirectoryService.Domain.PositionsMatrix;

public partial record PositionName
{
    public const int NAME_MIN_LENGHT = 3;
    public const int NAME_MAX_LENGHT = 150;

    public string Value { get; } = string.Empty;

    private PositionName() { }
    private PositionName(string value)
    {
        Value = value;
    }

    public static Result<PositionName, Error> Create(string value)
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

        return new PositionName(value);
    }

    [GeneratedRegex("\\s+", options: RegexOptions.Compiled)]
    private static partial Regex _manySpaces { get; }
    public override string ToString() => Value.ToString();
}
