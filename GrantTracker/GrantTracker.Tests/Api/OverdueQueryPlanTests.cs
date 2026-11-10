using GrantTracker.Api.Services;
using GrantTracker.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static GrantTracker.Tests.Api.ApiFactory;

namespace GrantTracker.Tests.Api;

public class OverdueQueryPlanTests
{
    [Fact]
    public void OverdueQuery_FindsTasksThroughTheIndex()
    {
        using var factory = new ApiFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GrantTrackerDbContext>();

        var plan = ExplainQueryPlan(db, TaskService.OverdueQuery(db, Today).ToQueryString());

        Assert.Contains(plan, step => step.Contains("USING INDEX IX_Tasks_CompletedOn_DueDate"));
        Assert.DoesNotContain(plan, step => step.StartsWith("SCAN"));
    }

    private static List<string> ExplainQueryPlan(GrantTrackerDbContext db, string queryString)
    {
        var lines = queryString.Split('\n', StringSplitOptions.TrimEntries);
        var connection = (SqliteConnection)db.Database.GetDbConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "EXPLAIN QUERY PLAN " + string.Join('\n', lines.Where(l => !l.StartsWith(".param")));

        foreach (var parameter in lines.Where(l => l.StartsWith(".param set ")))
        {
            var parts = parameter.Split(' ', 4);
            command.Parameters.AddWithValue(parts[2], parts[3].Trim('\''));
        }

        var plan = new List<string>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            plan.Add(reader.GetString(3));
        }

        return plan;
    }
}
