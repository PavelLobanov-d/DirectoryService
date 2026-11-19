using DirectoryService.Domain.shared;
using Xunit;

namespace DirectoryService.IntegrationTests;

public sealed class PathSlugTests
{
    [Theory]
    [InlineData("root")]
    [InlineData("root2")]
    [InlineData("root2.child3")]
    public void Create_ValidPath_Succeeds(string value)
    {
        var result = PathSlug.Create(value);

        Assert.True(result.IsSuccess);
        Assert.Equal(value, result.Value.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(".root")]
    [InlineData("root.")]
    [InlineData("root..child")]
    [InlineData("root-child")]
    [InlineData("Root.Child")]
    public void Create_InvalidPath_Fails(string value)
    {
        Assert.True(PathSlug.Create(value).IsFailure);
    }

    [Fact]
    public void Slug_AllowsDigits()
    {
        Assert.True(Slug.Create("dept2").IsSuccess);
    }

    [Fact]
    public void Slug_RejectsHyphen()
    {
        Assert.True(Slug.Create("dept-2").IsFailure);
    }
}
