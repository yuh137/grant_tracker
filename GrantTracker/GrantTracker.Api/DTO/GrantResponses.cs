using GrantTracker.Domain.Entities;
using GrantTracker.Domain.Enums;

namespace GrantTracker.Api.DTO;

public record GrantSummaryResponse(
    Guid Id,
    string Reference,
    string OrganisationName,
    DateOnly ApplicationDate,
    GrantStatus Status,
    int PendingTaskCount)
{
    public static GrantSummaryResponse From(Grant grant)
    {
        int pendingTaskCount = 0;
        foreach (var task in grant.Tasks)
        {
            if (task.IsPending)
            {
                pendingTaskCount++;
            }
        }

        return new GrantSummaryResponse(
            grant.Id,
            grant.Reference,
            grant.OrganisationName,
            grant.ApplicationDate,
            grant.Status,
            pendingTaskCount);
    }
}

public record GrantDetailResponse(
    Guid Id,
    string Reference,
    string OrganisationName,
    DateOnly ApplicationDate,
    GrantStatus Status,
    IReadOnlyList<TaskResponse> Tasks)
{
    public static GrantDetailResponse From(Grant grant, DateOnly today)
    {
        var taskResponses = new List<TaskResponse>();
        foreach (var task in grant.Tasks)
        {
            var taskResponse = TaskResponse.From(task, today);
            taskResponses.Add(taskResponse);
        }

        return new GrantDetailResponse(
            grant.Id,
            grant.Reference,
            grant.OrganisationName,
            grant.ApplicationDate,
            grant.Status,
            taskResponses);
    }
}
