using System.Linq.Expressions;

using GrantTracker.Domain.Enums;

namespace GrantTracker.Domain.Entities;

public class GrantTask
{
    public const int MaxTitleLength = 100;

    public Guid Id { get; set; }
    public Guid GrantId { get; set; }
    public Grant Grant { get; set; } = null!;
    public required string Title { get; set; }
    public TaskPriority Priority { get; set; }
    public DateOnly DueDate { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateOnly? CompletedOn { get; set; }

    public bool IsCompleted => CompletedOn != null;
    public bool IsPending => CompletedOn == null;

    public bool IsOverdue(DateOnly today) => IsPending && DueDate < today;

    public static Expression<Func<GrantTask, bool>> OverdueOn(DateOnly today) =>
        t => t.CompletedOn == null && t.DueDate < today;

    public bool Complete(DateOnly today)
    {
        if (IsCompleted)
        {
            return false;
        }

        CompletedOn = today;
        return true;
    }

    public static string ValidateTitle(string? title)
    {
        var trimmed = title?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            throw new BusinessRuleException(BusinessRuleErrorCode.InvalidTitle, "The task title is required.");
        }

        if (trimmed.Length > MaxTitleLength)
        {
            throw new BusinessRuleException(BusinessRuleErrorCode.InvalidTitle, $"The task title must be {MaxTitleLength} characters or fewer.");
        }

        return trimmed;
    }
}
