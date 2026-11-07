using GrantTracker.Domain.Entities;
using GrantTracker.Domain.Enums;

namespace GrantTracker.Data;

/// <summary>
/// Seed data from section 6 of the exercise. Dates are relative to the office's today so the
/// overdue logic can always be demonstrated. Ids are fixed so the .http file and tests can refer to them.
/// Rows are written directly rather than through the domain methods because the seed deliberately
/// contains tasks that are already overdue, which BR-2 would reject.
/// </summary>
public static class DatabaseSeeder
{
    public static class GrantIds
    {
        public static readonly Guid Riverside = new("00000000-0000-0000-0000-000000000001");
        public static readonly Guid Northside = new("00000000-0000-0000-0000-000000000002");
        public static readonly Guid Harbour = new("00000000-0000-0000-0000-000000000003");
        public static readonly Guid Hillview = new("00000000-0000-0000-0000-000000000004");
    }

    public static class TaskIds
    {
        public static readonly Guid RiversideVerifyBank = new("00000000-0000-0000-0000-000000000101");
        public static readonly Guid RiversideRequestQuote = new("00000000-0000-0000-0000-000000000102");
        public static readonly Guid RiversideCheckEligibility = new("00000000-0000-0000-0000-000000000103");
        public static readonly Guid NorthsideReviewReport = new("00000000-0000-0000-0000-000000000201");
        public static readonly Guid NorthsideScheduleVisit = new("00000000-0000-0000-0000-000000000202");
        public static readonly Guid HarbourFinalAcquittal = new("00000000-0000-0000-0000-000000000301");
    }

    public static void Seed(GrantTrackerDbContext db, TimeProvider timeProvider)
    {
        if (db.Grants.Any())
        {
            return;
        }

        var zone = timeProvider.LocalTimeZone;
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);

        // CreatedOn is a timestamp; for seed rows it is midnight, office time, on the application date.
        DateTimeOffset Midnight(DateOnly date)
        {
            var local = date.ToDateTime(TimeOnly.MinValue);
            return new DateTimeOffset(local, zone.GetUtcOffset(local));
        }

        // Assumptions not stated in the spec: a seed task was created on the grant's application date,
        // and a completed seed task was completed on its due date.
        var riversideApplied = today.AddDays(-60);
        var northsideApplied = today.AddDays(-20);
        var harbourApplied = today.AddDays(-200);
        var hillviewApplied = today.AddDays(-5);

        db.Grants.AddRange(
            new Grant
            {
                Id = GrantIds.Riverside,
                Reference = "GR-100001",
                OrganisationName = "Riverside Community Garden Inc.",
                ApplicationDate = riversideApplied,
                Status = GrantStatus.Open,
                Tasks =
                [
                    NewTask(TaskIds.RiversideVerifyBank, "Verify bank details", TaskPriority.High,
                        due: today.AddDays(-3), createdOn: Midnight(riversideApplied)),
                    NewTask(TaskIds.RiversideRequestQuote, "Request quote for tools", TaskPriority.Medium,
                        due: today.AddDays(5), createdOn: Midnight(riversideApplied)),
                    NewTask(TaskIds.RiversideCheckEligibility, "Check eligibility documents", TaskPriority.High,
                        due: today.AddDays(-50), createdOn: Midnight(riversideApplied), completedOn: today.AddDays(-50))
                ]
            },
            new Grant
            {
                Id = GrantIds.Northside,
                Reference = "GR-100002",
                OrganisationName = "Northside Youth Sports Club",
                ApplicationDate = northsideApplied,
                Status = GrantStatus.Open,
                Tasks =
                [
                    NewTask(TaskIds.NorthsideReviewReport, "Review progress report", TaskPriority.Low,
                        due: today.AddDays(-1), createdOn: Midnight(northsideApplied)),
                    NewTask(TaskIds.NorthsideScheduleVisit, "Schedule site visit", TaskPriority.Medium,
                        due: today.AddDays(10), createdOn: Midnight(northsideApplied))
                ]
            },
            new Grant
            {
                Id = GrantIds.Harbour,
                Reference = "GR-100003",
                OrganisationName = "Harbour Arts Collective",
                ApplicationDate = harbourApplied,
                Status = GrantStatus.Closed,
                Tasks =
                [
                    NewTask(TaskIds.HarbourFinalAcquittal, "Final acquittal review", TaskPriority.High,
                        due: today.AddDays(-150), createdOn: Midnight(harbourApplied), completedOn: today.AddDays(-150))
                ]
            },
            new Grant
            {
                Id = GrantIds.Hillview,
                Reference = "GR-100004",
                OrganisationName = "Hillview Library Friends Group",
                ApplicationDate = hillviewApplied,
                Status = GrantStatus.Open
            });

        db.SaveChanges();
    }

    private static GrantTask NewTask(Guid id, string title, TaskPriority priority, DateOnly due, DateTimeOffset createdOn,
        DateOnly? completedOn = null) =>
        new()
        {
            Id = id,
            Title = title,
            Priority = priority,
            DueDate = due,
            CreatedOn = createdOn,
            CompletedOn = completedOn
        };
}
