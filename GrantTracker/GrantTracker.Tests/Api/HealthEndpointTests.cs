using System.Net;
using System.Net.Http.Json;
using GrantTracker.Api.Health;
using static GrantTracker.Tests.Api.ApiFactory;

namespace GrantTracker.Tests.Api;

public class HealthEndpointTests : IDisposable
{
    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;

    public HealthEndpointTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task Live_Returns200WithoutTouchingDependencies()
    {
        var response = await _client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var health = await response.Content.ReadFromJsonAsync<HealthResponse>(Json);
        Assert.Equal("Healthy", health!.Status);
        Assert.Empty(health.Checks);
    }

    [Fact]
    public async Task Ready_Returns200WhenDatabaseAnswers()
    {
        var response = await _client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var health = await response.Content.ReadFromJsonAsync<HealthResponse>(Json);
        Assert.Equal("Healthy", health!.Status);
        var check = Assert.Single(health.Checks);
        Assert.Equal("database", check.Name);
        Assert.Equal("Healthy", check.Status);
    }

    [Fact]
    public async Task Ready_Returns503WhenDatabaseIsGoneButLiveStays200()
    {
        _factory.DropDatabase();

        var ready = await _client.GetAsync("/health/ready");
        var live = await _client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, ready.StatusCode);
        var health = await ready.Content.ReadFromJsonAsync<HealthResponse>(Json);
        Assert.Equal("Unhealthy", health!.Status);
        Assert.Equal("Unhealthy", Assert.Single(health.Checks).Status);
        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
    }
}
