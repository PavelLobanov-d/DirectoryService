using CSharpFunctionalExtensions;
using DirectoryService.Domain.shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace DirectoryService.Domain.Locations;

public partial record Address
{
    public const int NAME_MIN_LENGHT = 3;
    public const int NAME_MAX_LENGHT = 150;

    public string Value { get; } = string.Empty;

    private Address() { }
    public Address(string value)
    {
        Value = value;
    }

    public static Result<Address, Error> Create(string value)
    {
        string normalized = _manySpaces.Replace(input:value.Trim(), " ");

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return GeneralErrors.ValueIsRequired("адрес");
        }

        if (normalized.Length is < NAME_MIN_LENGHT or > NAME_MAX_LENGHT)
        {
            return GeneralErrors.ValueIsInvalid("адрес");
        }
        return new Address(normalized);
    }

    [GeneratedRegex("\\s+", options: RegexOptions.Compiled)]
    private static partial Regex _manySpaces { get; }
}
