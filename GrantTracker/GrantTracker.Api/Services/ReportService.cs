using GrantTracker.Api.DTO;
using GrantTracker.Data;
using Microsoft.EntityFrameworkCore;

namespace GrantTracker.Api.Services;

public class ReportService(GrantTrackerDbContext db, TimeProvider timeProvider)
{
    public async Task<TaskActivityReport> TaskActivityAsync(int days, CancellationToken ct)
    {
        var to = timeProvider.Today();
        var from = to.AddDays(1 - days);

        var created = await CreatedPerOfficeDayAsync(from, to, ct);
        var completed = await CompletedPerDayAsync(from, to, ct);

        var rows = new List<DailyTaskActivity>(days);
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            rows.Add(new DailyTaskActivity(date, created.GetValueOrDefault(date), completed.GetValueOrDefault(date)));
        }

        var overdueList = await MeasureOverdueListAsync(to, ct);
        return new TaskActivityReport(from, to, rows, overdueList);
    }

    private async Task<Dictionary<DateOnly, int>> CreatedPerOfficeDayAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        var createdOn = await db.Tasks.AsNoTracking().Select(t => t.CreatedOn).ToListAsync(ct);
        var officeZone = timeProvider.LocalTimeZone;

        return createdOn
            .Select(moment => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(moment, officeZone).DateTime))
            .Where(date => date >= from && date <= to)
            .CountBy(date => date)
            .ToDictionary();
    }

    private async Task<Dictionary<DateOnly, int>> CompletedPerDayAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        var counts = await db.Tasks
            .AsNoTracking()
            .Where(t => t.CompletedOn >= from && t.CompletedOn <= to)
            .GroupBy(t => t.CompletedOn)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return counts.ToDictionary(c => c.Date!.Value, c => c.Count);
    }

    private async Task<OverdueListSnapshot> MeasureOverdueListAsync(DateOnly today, CancellationToken ct)
    {
        var totalTasks = await db.Tasks.CountAsync(ct);

        var started = timeProvider.GetTimestamp();
        var overdue = await TaskService.OverdueQuery(db, today).ToListAsync(ct);
        var elapsed = timeProvider.GetElapsedTime(started);

        return new OverdueListSnapshot(totalTasks, overdue.Count, Math.Round(elapsed.TotalMilliseconds, 2));
    }
}
