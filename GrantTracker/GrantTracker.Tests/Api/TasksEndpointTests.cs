using System.Net;
using System.Net.Http.Json;
using GrantTracker.Api.DTO;
using GrantTracker.Data;
using static GrantTracker.Tests.Api.ApiFactory;

namespace GrantTracker.Tests.Api;

public class TasksEndpointTests : IDisposable
{
    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;

    public TasksEndpointTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task Overdue_ReturnsPendingPastDueTasksMostOverdueFirst()
    {
        var overdue = await _client.GetFromJsonAsync<List<OverdueTaskResponse>>("/tasks/overdue", Json);

        Assert.NotNull(overdue);
        Assert.Equal(["Verify bank details", "Review progress report"], overdue.Select(t => t.Title));
        Assert.Equal([3, 1], overdue.Select(t => t.DaysOverdue));
        Assert.Equal(["GR-100001", "GR-100002"], overdue.Select(t => t.GrantReference));
    }

    [Fact]
    public async Task Complete_RecordsTodayAndRepeatOnLaterDayKeepsOriginalDate()
    {
        var url = $"/tasks/{DatabaseSeeder.TaskIds.NorthsideReviewReport}/complete";

        var first = await _client.PutAsync(url, null);
        _factory.Clock.Advance(TimeSpan.FromDays(2));
        var second = await _client.PutAsync(url, null);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var firstTask = await first.Content.ReadFromJsonAsync<TaskResponse>(Json);
        var secondTask = await second.Content.ReadFromJsonAsync<TaskResponse>(Json);
        Assert.Equal(Today, firstTask!.CompletedOn);
        Assert.Equal(Today, secondTask!.CompletedOn);
        Assert.False(secondTask.IsOverdue);

        var overdue = await _client.GetFromJsonAsync<List<OverdueTaskResponse>>("/tasks/overdue", Json);
        Assert.DoesNotContain(overdue!, t => t.Id == DatabaseSeeder.TaskIds.NorthsideReviewReport);
    }

    [Fact]
    public async Task Overdue_MovesWithTheClock()
    {
        _factory.Clock.Advance(TimeSpan.FromDays(6));

        var overdue = await _client.GetFromJsonAsync<List<OverdueTaskResponse>>("/tasks/overdue", Json);

        Assert.Contains(overdue!, t => t.Id == DatabaseSeeder.TaskIds.RiversideRequestQuote);
        Assert.Equal(1, overdue!.Single(t => t.Id == DatabaseSeeder.TaskIds.RiversideRequestQuote).DaysOverdue);
    }

    [Fact]
    public async Task Overdue_ExcludesTaskDueToday()
    {
        var response = await _client.PostAsJsonAsync($"/grants/{DatabaseSeeder.GrantIds.Hillview}/tasks",
            new { title = "Due today", priority = "High", dueDate = Today }, Json);
        var created = await response.Content.ReadFromJsonAsync<TaskResponse>(Json);

        var overdue = await _client.GetFromJsonAsync<List<OverdueTaskResponse>>("/tasks/overdue", Json);

        Assert.DoesNotContain(overdue!, t => t.Id == created!.Id);
        _factory.Clock.Advance(TimeSpan.FromDays(1));
        var overdueTomorrow = await _client.GetFromJsonAsync<List<OverdueTaskResponse>>("/tasks/overdue", Json);
        Assert.Contains(overdueTomorrow!, t => t.Id == created!.Id);
    }

    [Fact]
    public async Task Complete_UnknownId_Returns404()
    {
        var response = await _client.PutAsync($"/tasks/{Guid.NewGuid()}/complete", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
