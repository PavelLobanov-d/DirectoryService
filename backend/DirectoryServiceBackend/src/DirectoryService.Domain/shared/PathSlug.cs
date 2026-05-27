using System;
using System.Collections.Generic;
using System.Text;

namespace DirectoryService.Domain.shared;

public sealed record PathSlug
{
    private const char SEPARATOR = '.';

    public string Value { get; }

    private PathSlug(string value)
    {
        Value = value;
    }

    public static PathSlug Create(Slug slug)
    {
        return new PathSlug(slug.Value);
    }

    public PathSlug CreateChild(Slug childSlug)
    {
        return new PathSlug(Value + SEPARATOR + childSlug.Value);
    }
}
