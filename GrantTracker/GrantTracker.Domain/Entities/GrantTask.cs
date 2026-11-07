using System.Linq.Expressions;

using GrantTracker.Domain.Enums;

namespace GrantTracker.Domain.Entities;

/// <summary>
/// A unit of work an officer must do on a grant. Named GrantTask to avoid clashing with System.Threading.Tasks.Task.
/// Completion is a date, not a flag: BR-4 needs the date, and "completed" is derived from it.
/// </summary>
public class GrantTask
{
    public const int MaxTitleLength = 100;

    public Guid Id { get; set; }
    public Guid GrantId { get; set; }
    public Grant Grant { get; set; } = null!;
    public required string Title { get; set; }
    public TaskPriority Priority { get; set; }
    public DateOnly DueDate { get; set; }
    /// <summary>When the row was created: a system timestamp, so an instant, unlike the calendar dates above.</summary>
    public DateTimeOffset CreatedOn { get; set; }
    public DateOnly? CompletedOn { get; set; }

    public bool IsCompleted => CompletedOn is not null;
    public bool IsPending => CompletedOn is null;

    /// <summary>BR-6: pending and due strictly before today. Due today is not overdue.</summary>
    public bool IsOverdue(DateOnly today) => IsPending && DueDate < today;

    /// <summary>
    /// BR-6 as a query predicate so EF Core can run it in SQL. Must stay in step with <see cref="IsOverdue"/>;
    /// a unit test checks that they agree.
    /// </summary>
    public static Expression<Func<GrantTask, bool>> OverdueOn(DateOnly today) =>
        t => t.CompletedOn == null && t.DueDate < today;

    /// <summary>
    /// BR-4: records the completion date the first time only. A second call changes nothing.
    /// Returns true when state changed, so callers can tell a no-op from a first completion.
    /// </summary>
    public bool Complete(DateOnly today)
    {
        if (IsCompleted)
        {
            return false;
        }

        CompletedOn = today;
        return true;
    }

    /// <summary>BR-3: title is required and at most <see cref="MaxTitleLength"/> characters after trimming.</summary>
    public static string ValidateTitle(string? title)
    {
        var trimmed = title?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            throw new BusinessRuleException("BR-3", "The task title is required.");
        }

        if (trimmed.Length > MaxTitleLength)
        {
            throw new BusinessRuleException("BR-3", $"The task title must be {MaxTitleLength} characters or fewer.");
        }

        return trimmed;
    }
}
