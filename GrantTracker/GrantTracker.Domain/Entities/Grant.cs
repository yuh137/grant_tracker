using GrantTracker.Domain.Enums;

namespace GrantTracker.Domain.Entities;

/// <summary>
/// Aggregate root. All changes to a grant or its task list go through methods here so the
/// business rules have exactly one home and can be unit-tested without a database.
/// </summary>
public class Grant
{
    public Guid Id { get; set; }
    public required string Reference { get; set; }
    public required string OrganisationName { get; set; }
    public DateOnly ApplicationDate { get; set; }
    public GrantStatus Status { get; set; } = GrantStatus.Open;

    /// <summary>
    /// Optimistic concurrency token. Every method that changes the grant or its task list bumps it,
    /// so EF Core's UPDATE ... WHERE Version = @original fails if another officer changed the grant
    /// in between. This is what keeps BR-1 and BR-5 true under concurrent use (OP-2).
    /// </summary>
    public int Version { get; set; }

    public List<GrantTask> Tasks { get; set; } = [];

    public bool IsClosed => Status == GrantStatus.Closed;

    /// <summary>
    /// Adds a task, enforcing BR-1, BR-2 and BR-3. <paramref name="now"/> is the current time in the
    /// office's time zone: its date is "today" for BR-2 and the instant is recorded as CreatedOn.
    /// </summary>
    public GrantTask AddTask(string? title, TaskPriority priority, DateOnly dueDate, DateTimeOffset now)
    {
        var today = DateOnly.FromDateTime(now.DateTime);

        if (IsClosed)
        {
            throw new BusinessRuleException("BR-1", "A task cannot be added to a closed grant.");
        }

        var validTitle = GrantTask.ValidateTitle(title);

        if (dueDate < today)
        {
            throw new BusinessRuleException("BR-2", "The due date cannot be in the past.");
        }

        if (dueDate < ApplicationDate)
        {
            throw new BusinessRuleException("BR-2", "The due date cannot be before the grant's application date.");
        }

        var task = new GrantTask
        {
            Id = Guid.NewGuid(),
            GrantId = Id,
            Grant = this,
            Title = validTitle,
            Priority = priority,
            DueDate = dueDate,
            CreatedOn = now
        };

        Tasks.Add(task);
        Version++;
        return task;
    }

    /// <summary>
    /// Closes the grant, enforcing BR-5. Requires <see cref="Tasks"/> to be loaded.
    /// Closing an already-closed grant is a no-op, for the same reason as BR-4: a retried request must be safe.
    /// </summary>
    public bool Close()
    {
        if (IsClosed)
        {
            return false;
        }

        if (Tasks.Any(t => t.IsPending && t.Priority == TaskPriority.High))
        {
            throw new BusinessRuleException("BR-5", "A grant cannot be closed while it has a pending High-priority task.");
        }

        Status = GrantStatus.Closed;
        Version++;
        return true;
    }
}
