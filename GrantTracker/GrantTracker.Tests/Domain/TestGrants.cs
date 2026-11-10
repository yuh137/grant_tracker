using GrantTracker.Domain.Entities;
using GrantTracker.Domain.Enums;

namespace GrantTracker.Tests.Domain;

public static class TestGrants
{
    public static readonly DateOnly TestToday = new(2026, 3, 15);
    public static readonly DateTimeOffset TestNow = new(2026, 3, 15, 10, 30, 0, TimeSpan.FromHours(11));

    public static Grant TestGrantOpen(DateOnly? applicationDate = null) => new()
    {
        Id = Guid.NewGuid(),
        Reference = "GR-TEST",
        OrganisationName = "Test Organisation",
        ApplicationDate = applicationDate ?? TestToday.AddDays(-60),
        Status = GrantStatus.Open
    };

    public static Grant TestGrantClose()
    {
        var grant = TestGrantOpen();
        grant.Status = GrantStatus.Closed;
        return grant;
    }

    public static GrantTask NewTestTask(Grant grant, TaskPriority priority, DateOnly dueDate, DateOnly? completedOn = null)
    {
        var task = new GrantTask
        {
            Id = Guid.NewGuid(),
            GrantId = grant.Id,
            Grant = grant,
            Title = "Test task",
            Priority = priority,
            DueDate = dueDate,
            CreatedOn = TestNow,
            CompletedOn = completedOn
        };
        grant.Tasks.Add(task);
        return task;
    }
}
