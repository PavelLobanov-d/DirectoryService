using CSharpFunctionalExtensions;
using DirectoryService.Domain.PositionsMatrix;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DirectoryService.Domain.shared;

public sealed partial record Slug
{
    public const int IDENTIFIER_MIN_LENGHT = 1;
    public const int IDENTIFIER_MAX_LENGHT = 150;

    public string Value { get; }

    private Slug() { }
    private Slug(string value)
    {
        Value = value;
    }

    public static Result<Slug, Error> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return GeneralErrors.ValueIsRequired("slug");
        }
        if (!_slugRegex.IsMatch(value))
        {
            return GeneralErrors.ValueIsInvalid("slug");
        }
        return new Slug(value);
    }

    [GeneratedRegex("^[a-z]+$", options: RegexOptions.Compiled)]
    private static partial Regex _slugRegex { get; }
    public override string ToString() => Value.ToString();
}
