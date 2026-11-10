using System.Net;
using System.Net.Http.Json;
using GrantTracker.Api.DTO;
using GrantTracker.Data;
using static GrantTracker.Tests.Api.ApiFactory;

namespace GrantTracker.Tests.Api;

public class ReportsEndpointTests : IDisposable
{
    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;

    public ReportsEndpointTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task TaskActivity_CountsSeededTasksOnTheDayTheyHappened()
    {
        var report = await _client.GetFromJsonAsync<TaskActivityReport>("/reports/task-activity?days=366", Json);

        Assert.Equal(Today.AddDays(-365), report!.From);
        Assert.Equal(Today, report.To);
        Assert.Equal(366, report.Days.Count);
        Assert.Equal(report.Days.OrderBy(d => d.Date), report.Days);

        var created = report.Days.Where(d => d.Created > 0).Select(d => (d.Date, d.Created));
        Assert.Equal([(Today.AddDays(-200), 1), (Today.AddDays(-60), 3), (Today.AddDays(-20), 2)], created);

        var completed = report.Days.Where(d => d.Completed > 0).Select(d => (d.Date, d.Completed));
        Assert.Equal([(Today.AddDays(-150), 1), (Today.AddDays(-50), 1)], completed);
    }

    [Fact]
    public async Task TaskActivity_CountsTodaysWorkOnceEvenWhenCompleteIsRepeated()
    {
        await _client.PostAsJsonAsync($"/grants/{DatabaseSeeder.GrantIds.Hillview}/tasks",
            new { title = "Confirm incorporation certificate", priority = "Medium", dueDate = Today }, Json);
        var completeUrl = $"/tasks/{DatabaseSeeder.TaskIds.NorthsideReviewReport}/complete";
        await _client.PutAsync(completeUrl, null);
        await _client.PutAsync(completeUrl, null);

        var report = await _client.GetFromJsonAsync<TaskActivityReport>("/reports/task-activity", Json);

        Assert.Equal(14, report!.Days.Count);
        Assert.Equal(new DailyTaskActivity(Today, Created: 1, Completed: 1), report.Days[^1]);
        Assert.Equal(7, report.OverdueList.TotalTasks);
        Assert.Equal(1, report.OverdueList.OverdueTasks);
        Assert.True(report.OverdueList.QueryMilliseconds >= 0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(367)]
    public async Task TaskActivity_DaysOutOfRange_Returns400(int days)
    {
        var response = await _client.GetAsync($"/reports/task-activity?days={days}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>(Json);
        Assert.Contains("days", problem!.Errors!.Keys);
    }

    [Fact]
    public async Task TaskActivity_FollowsTheOfficeCalendarNotUtc()
    {
        using var factory = new ApiFactory();
        factory.Clock.SetLocalTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Australia/Sydney"));
        using var client = factory.CreateClient();
        var officeToday = Today.AddDays(1);

        await client.PostAsJsonAsync($"/grants/{DatabaseSeeder.GrantIds.Hillview}/tasks",
            new { title = "Book the site visit", priority = "Low", dueDate = officeToday }, Json);

        var report = await client.GetFromJsonAsync<TaskActivityReport>("/reports/task-activity?days=1", Json);

        Assert.Equal(officeToday, report!.To);
        Assert.Equal(new DailyTaskActivity(officeToday, Created: 1, Completed: 0), report.Days.Single());
    }
}
