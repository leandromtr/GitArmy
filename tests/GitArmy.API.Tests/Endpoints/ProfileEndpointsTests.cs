using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

namespace GitArmy.API.Tests.Endpoints;

public class ProfileEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ProfileEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("invalid!user")]
    [InlineData("")]
    [InlineData("this-username-is-way-too-long-and-exceeds-the-39-char-limit")]
    public async Task AnalyseProfile_InvalidUsername_ReturnsBadRequest(string username)
    {
        var response = await _client.PostAsJsonAsync("/api/profiles/analyse", new { Username = username });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetProfile_NonExistentUsername_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/profiles/this-user-does-not-exist-xyz");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetRanking_DefaultCount_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/ranking");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
