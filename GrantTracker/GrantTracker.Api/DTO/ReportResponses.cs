namespace GrantTracker.Api.DTO;

public record TaskActivityReport(
    DateOnly From,
    DateOnly To,
    IReadOnlyList<DailyTaskActivity> Days,
    OverdueListSnapshot OverdueList);

public record DailyTaskActivity(DateOnly Date, int Created, int Completed);

public record OverdueListSnapshot(int TotalTasks, int OverdueTasks, double QueryMilliseconds);
