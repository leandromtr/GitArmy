using FluentAssertions;
using GitArmy.Domain.ValueObjects;

namespace GitArmy.Domain.Tests.ValueObjects;

public class GitHubUsernameTests
{
    [Theory]
    [InlineData("torvalds")]
    [InlineData("sindresorhus")]
    [InlineData("a")]
    [InlineData("user-name")]
    [InlineData("user123")]
    public void Create_ValidUsername_ReturnsNormalized(string input)
    {
        var result = GitHubUsername.Create(input);
        result.Value.Should().Be(input.ToLowerInvariant());
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("-startshyphen")]
    [InlineData("endshyphen-")]
    [InlineData("double--hyphen")]
    [InlineData("this-username-is-way-too-long-exceeding-39-chars")]
    [InlineData("invalid!char")]
    public void Create_InvalidUsername_ThrowsArgumentException(string input)
    {
        var act = () => GitHubUsername.Create(input);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_TrimsAndLowercases()
    {
        var result = GitHubUsername.Create("  LeandroReis  ");
        result.Value.Should().Be("leandroreis");
    }

    [Fact]
    public void Equality_SameValue_IsEqual()
    {
        var a = GitHubUsername.Create("torvalds");
        var b = GitHubUsername.Create("TORVALDS");
        a.Should().Be(b);
    }
}
