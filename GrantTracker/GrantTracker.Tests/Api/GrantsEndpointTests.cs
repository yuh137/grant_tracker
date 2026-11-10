using System.Net;
using System.Net.Http.Json;
using GrantTracker.Api.DTO;
using GrantTracker.Data;
using GrantTracker.Domain.Enums;
using static GrantTracker.Tests.Api.ApiFactory;

namespace GrantTracker.Tests.Api;

public class GrantsEndpointTests : IDisposable
{
    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;

    public GrantsEndpointTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task ReturnsSeededGrantsWithPendingTaskCounts()
    {
        var grants = await _client.GetFromJsonAsync<List<GrantSummaryResponse>>("/grants", Json);

        Assert.NotNull(grants);
        Assert.Equal(4, grants.Count);
        Assert.Equal(["GR-100001", "GR-100002", "GR-100003", "GR-100004"], grants.Select(g => g.Reference));
        Assert.Equal(2, grants.Single(g => g.Id == DatabaseSeeder.GrantIds.Riverside).PendingTaskCount);
        Assert.Equal(0, grants.Single(g => g.Id == DatabaseSeeder.GrantIds.Harbour).PendingTaskCount);
        Assert.Equal(GrantStatus.Closed, grants.Single(g => g.Id == DatabaseSeeder.GrantIds.Harbour).Status);
    }

    [Theory]
    [InlineData("Open", 3)]
    [InlineData("closed", 1)]
    public async Task FiltersByStatus(string status, int expectedCount)
    {
        var grants = await _client.GetFromJsonAsync<List<GrantSummaryResponse>>($"/grants?status={status}", Json);

        Assert.NotNull(grants);
        Assert.Equal(expectedCount, grants.Count);
    }

    [Fact]
    public async Task Get_ReturnsTasksOrderedByDueDateWithOverdueFlags()
    {
        var grant = await _client.GetFromJsonAsync<GrantDetailResponse>(
            $"/grants/{DatabaseSeeder.GrantIds.Riverside}", Json);

        Assert.NotNull(grant);
        Assert.Equal("Riverside Community Garden Inc.", grant.OrganisationName);
        Assert.Equal(Today.AddDays(-60), grant.ApplicationDate);
        Assert.Equal(
            ["Check eligibility documents", "Verify bank details", "Request quote for tools"],
            grant.Tasks.Select(t => t.Title));
        Assert.Equal([false, true, false], grant.Tasks.Select(t => t.IsOverdue));
        Assert.Equal([true, false, false], grant.Tasks.Select(t => t.IsCompleted));
    }

    [Fact]
    public async Task Get_UnknownId_Returns404ProblemDetails()
    {
        var response = await _client.GetAsync($"/grants/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>(Json);
        Assert.Equal("Resource not found", problem!.Title);
    }

    [Fact]
    public async Task AddTask_Valid_Returns201WithLocationAndPersists()
    {
        var grantId = DatabaseSeeder.GrantIds.Hillview;

        var response = await _client.PostAsJsonAsync($"/grants/{grantId}/tasks",
            new { title = "  Confirm insurance  ", priority = "Medium", dueDate = Today.AddDays(7) }, Json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains(grantId.ToString(), response.Headers.Location!.ToString());
        var task = await response.Content.ReadFromJsonAsync<TaskResponse>(Json);
        Assert.Equal("Confirm insurance", task!.Title);
        Assert.Equal(TaskPriority.Medium, task.Priority);
        Assert.Equal(_factory.Clock.GetUtcNow(), task.CreatedOn);
        Assert.False(task.IsOverdue);

        var grant = await _client.GetFromJsonAsync<GrantDetailResponse>($"/grants/{grantId}", Json);
        Assert.Single(grant!.Tasks, t => t.Id == task.Id);
    }

    [Fact]
    public async Task AddTask_MissingPriority_Returns400ValidationProblem()
    {
        var response = await _client.PostAsJsonAsync($"/grants/{DatabaseSeeder.GrantIds.Hillview}/tasks",
            new { title = "No priority", dueDate = Today }, Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>(Json);
        Assert.Contains("Priority", problem!.Errors!.Keys);
    }

    [Fact]
    public async Task AddTask_UnknownPriority_Returns400()
    {
        var response = await _client.PostAsJsonAsync($"/grants/{DatabaseSeeder.GrantIds.Hillview}/tasks",
            new { title = "Bad enum", priority = "Urgent", dueDate = Today }, Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddTask_UnknownGrant_Returns404()
    {
        var response = await _client.PostAsJsonAsync($"/grants/{Guid.NewGuid()}/tasks",
            new { title = "Orphan", priority = "Low", dueDate = Today }, Json);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000003", "Task on closed grant", 1, "GrantClosed")]
    [InlineData("00000000-0000-0000-0000-000000000004", "Task due yesterday", -1, "InvalidDueDate")]
    [InlineData("00000000-0000-0000-0000-000000000004", "   ", 1, "InvalidTitle")]
    public async Task AddTask_RuleViolation_Returns422WithCode(string grantId, string title, int dueDaysFromToday,
        string expectedCode)
    {
        var response = await _client.PostAsJsonAsync($"/grants/{grantId}/tasks",
            new { title, priority = "Low", dueDate = Today.AddDays(dueDaysFromToday) }, Json);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>(Json);
        Assert.Equal(expectedCode, problem!.Code);
        Assert.Equal("Business rule violated", problem.Title);
    }

    [Fact]
    public async Task Close_WithPendingHighTask_Returns422WithCode()
    {
        var response = await _client.PutAsync($"/grants/{DatabaseSeeder.GrantIds.Riverside}/close", null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>(Json);
        Assert.Equal("PendingHighPriorityTask", problem!.Code);
    }

    [Fact]
    public async Task Close_Grant_ClosesAndRepeatIsIdempotent()
    {
        var url = $"/grants/{DatabaseSeeder.GrantIds.Northside}/close";

        var first = await _client.PutAsync(url, null);
        var second = await _client.PutAsync(url, null);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var grant = await second.Content.ReadFromJsonAsync<GrantSummaryResponse>(Json);
        Assert.Equal(GrantStatus.Closed, grant!.Status);
    }
}
