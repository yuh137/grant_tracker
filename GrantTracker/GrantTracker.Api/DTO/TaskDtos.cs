using System.ComponentModel.DataAnnotations;
using GrantTracker.Domain.Entities;
using GrantTracker.Domain.Enums;

namespace GrantTracker.Api.DTO;

public class CreateTaskRequest
{
    public string? Title { get; set; }

    [Required]
    public TaskPriority? Priority { get; set; }

    [Required]
    public DateOnly? DueDate { get; set; }
}

public record TaskResponse(
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
    public static TaskResponse From(GrantTask task, DateOnly today)
    {
        bool isOverdue = task.IsOverdue(today);

        return new TaskResponse(
            task.Id,
            task.GrantId,
            task.Title,
            task.Priority,
            task.DueDate,
            task.CreatedOn,
            task.CompletedOn,
            task.IsCompleted,
            isOverdue);
    }
}

public record OverdueTaskResponse(
    Guid Id,
    Guid GrantId,
    string GrantReference,
    string OrganisationName,
    string Title,
    TaskPriority Priority,
    DateOnly DueDate,
    int DaysOverdue)
{
    public static OverdueTaskResponse From(GrantTask task, DateOnly today)
    {
        int daysOverdue = today.DayNumber - task.DueDate.DayNumber;

        return new OverdueTaskResponse(
            task.Id,
            task.GrantId,
            task.Grant.Reference,
            task.Grant.OrganisationName,
            task.Title,
            task.Priority,
            task.DueDate,
            daysOverdue);
    }
}
