using System.ComponentModel.DataAnnotations;
using GrantTracker.Domain.Entities;
using GrantTracker.Domain.Enums;

namespace GrantTracker.Api.Contracts;

public sealed class CreateTaskRequest
{
    public string? Title { get; init; }

    [Required]
    public TaskPriority? Priority { get; init; }

    [Required]
    public DateOnly? DueDate { get; init; }
}

public sealed record TaskResponse(
    Guid Id,
    Guid GrantId,
    string Title,
    TaskPriority Priority,
    DateOnly DueDate,
    DateTimeOffset CreatedOn,
    DateOnly? CompletedOn,
    bool IsCompleted,
    bool IsOverdue)
{
    public static TaskResponse From(GrantTask task, DateOnly today) =>
        new(task.Id, task.GrantId, task.Title, task.Priority, task.DueDate, task.CreatedOn, task.CompletedOn,
            task.IsCompleted, task.IsOverdue(today));
}

public sealed record OverdueTaskResponse(
    Guid Id,
    Guid GrantId,
    string GrantReference,
    string OrganisationName,
    string Title,
    TaskPriority Priority,
    DateOnly DueDate,
    int DaysOverdue)
{
    public static OverdueTaskResponse From(GrantTask task, DateOnly today) =>
        new(task.Id, task.GrantId, task.Grant.Reference, task.Grant.OrganisationName, task.Title, task.Priority,
            task.DueDate, today.DayNumber - task.DueDate.DayNumber);
}
