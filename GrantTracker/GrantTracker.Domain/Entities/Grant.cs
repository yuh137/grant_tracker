using System.ComponentModel.DataAnnotations;
using GrantTracker.Domain.Enums;

namespace GrantTracker.Domain.Entities;

public class Grant
{
    public Guid Id { get; set; }
    public required string Reference { get; set; }
    public required string OrganisationName { get; set; }
    public DateOnly ApplicationDate { get; set; }
    public GrantStatus Status { get; set; } = GrantStatus.Open;

    public int Version { get; set; }

    public List<GrantTask> Tasks { get; set; } = [];

    public bool IsClosed => Status == GrantStatus.Closed;

    public GrantTask AddTask(string? title, TaskPriority priority, DateOnly dueDate, DateTimeOffset now)
    {
        var today = DateOnly.FromDateTime(now.DateTime);

        if (IsClosed)
        {
            throw new BusinessRuleException(BusinessRuleErrorCode.GrantClosed, "A task cannot be added to a closed grant.");
        }

        var validTitle = GrantTask.ValidateTitle(title);

        if (dueDate < today)
        {
            throw new BusinessRuleException(BusinessRuleErrorCode.InvalidDueDate, "The due date cannot be in the past.");
        }

        if (dueDate < ApplicationDate)
        {
            throw new BusinessRuleException(BusinessRuleErrorCode.InvalidDueDate, "The due date cannot be before the grant's application date.");
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

    public bool Close()
    {
        if (IsClosed)
        {
            return false;
        }

        if (Tasks.Any(t => t.IsPending && t.Priority == TaskPriority.High))
        {
            throw new BusinessRuleException(BusinessRuleErrorCode.PendingHighPriorityTask, "A grant cannot be closed while it has a pending High-priority task.");
        }

        Status = GrantStatus.Closed;
        Version++;
        return true;
    }
}
