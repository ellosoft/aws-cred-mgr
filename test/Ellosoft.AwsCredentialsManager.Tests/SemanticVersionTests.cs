// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Infrastructure;
using FluentAssertions;

namespace Ellosoft.AwsCredentialsManager.Tests;

public class SemanticVersionTests
{
    [Theory]
    [InlineData("0.0.1")]
    [InlineData("0.0.1-alpha")]
    [InlineData("0.0.1-alpha.1")]
    [InlineData("0.0.1-alpha.1+data")]
    [InlineData("0.0.1.1+data")]
    [InlineData("0.0.1.1")]
    [InlineData("0.0.1-alpha-test")]
    [InlineData("0.0.1-alpha-test.1")]
    public void TryParse_Valid_Tests(string versionValue)
    {
        var result = SemanticVersion.TryParse(versionValue, out var actualVersion);

        Assert.True(result);
        Assert.Equal(versionValue, actualVersion!.ToString());
    }

    [Theory]
    [InlineData("0.0.1.0.1")]
    [InlineData("0.0.1.-")]
    [InlineData("0.0.1-alpha.0.1")]
    public void TryParse_Invalid_Tests(string versionValue)
    {
        var result = SemanticVersion.TryParse(versionValue, out var actualVersion);

        Assert.False(result);
    }

    [Theory]
    [InlineData("0.0.1", "0.0.2", -1)]
    [InlineData("0.0.1", "0.0.1", 0)]
    [InlineData("0.0.2", "0.0.1", 1)]
    [InlineData("0.0.1", "0.0.1-alpha", 1)]
    [InlineData("0.0.1-alpha", "0.0.1-beta", -1)]
    [InlineData("0.0.1-alpha", "0.0.1-beta.2", -1)]
    [InlineData("0.0.1-alpha.1", "0.0.1-alpha.2", -1)]
    [InlineData("0.0.1-alpha", "0.0.1-alpha.1", -1)]
    [InlineData("0.0.1-alpha.1", "0.0.1-alpha.1", 0)]
    [InlineData("0.0.1+data", "0.0.1+data.1", 0)]
    [InlineData("0.0.1-alpha.1+data", "0.0.1-alpha.1", 0)]
    public void CompareTo_Tests(string version1, string version2, int expectedResult)
    {
        SemanticVersion.TryParse(version1, out var v1);
        SemanticVersion.TryParse(version2, out var v2);

        var result = v1!.CompareTo(v2);

        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public void OrderBy_WhenVersionsAreUnsorted_ShouldSortVersions()
    {
        var unsortedData = new[]
        {
            "0.1",
            "0.0.1",
            "0.0.2",
            "1.0.1",
            "0.0.1-alpha",
            "0.0.1-beta",
            "10.0.1",
            "0.0.1-alpha.1"
        }.Select(v => new SemanticVersion(v));

        var sortedData = unsortedData.Order().ToList();

        var expectedResult = new[]
        {
            "0.0.1-alpha",
            "0.0.1-alpha.1",
            "0.0.1-beta",
            "0.0.1",
            "0.0.2",
            "0.1",
            "1.0.1",
            "10.0.1"
        }.Select(v => new SemanticVersion(v));

        sortedData.Should().BeInAscendingOrder();
        sortedData.Should().Equal(expectedResult);
    }

    [Theory]
    [InlineData("0.0.1", "0.0.1")]
    [InlineData("0.0.1-alpha.1+data.1", "0.0.1-alpha.1+data.1")]
    public void Equal_Tests(string v1, string v2)
    {
        var result = new SemanticVersion(v1) == new SemanticVersion(v2);
        Assert.True(result);
    }
}
