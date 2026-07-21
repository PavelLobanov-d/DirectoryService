using CSharpFunctionalExtensions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace DirectoryService.Domain.shared;

public sealed partial record PathSlug
{
    private const char SEPARATOR = '.';

    public string Value { get; }

    private PathSlug() { }
    private PathSlug(string value)
    {
        Value = value;
    }

    public static Result<PathSlug, Error> Create(Slug slug)
    {
        return new PathSlug(slug.Value);
    }
    public static Result<PathSlug, Error> Create(string value)
    {
        if (!_slugPathRegex.IsMatch(value))
        {
            return GeneralErrors.ValueIsInvalid("value");
        }
        return new PathSlug(value);
    }
    public Result<PathSlug, Error> CreateChild(Slug childSlug)
    {
        return new PathSlug(Value + SEPARATOR + childSlug.Value);
    }

    [GeneratedRegex("^[a-z.]+$", options: RegexOptions.Compiled)]
    private static partial Regex _slugPathRegex { get; }
    public override string ToString() => Value.ToString();

}
