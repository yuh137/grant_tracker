using GrantTracker.Domain.Entities;
using GrantTracker.Domain.Enums;

namespace GrantTracker.Api.Contracts;

public sealed record GrantSummaryResponse(
    Guid Id,
    string Reference,
    string OrganisationName,
    DateOnly ApplicationDate,
    GrantStatus Status,
    int PendingTaskCount)
{
    public static GrantSummaryResponse From(Grant grant) =>
        new(grant.Id, grant.Reference, grant.OrganisationName, grant.ApplicationDate, grant.Status,
            grant.Tasks.Count(t => t.IsPending));
}

public sealed record GrantDetailResponse(
    Guid Id,
    string Reference,
    string OrganisationName,
    DateOnly ApplicationDate,
    GrantStatus Status,
    IReadOnlyList<TaskResponse> Tasks)
{
    public static GrantDetailResponse From(Grant grant, DateOnly today) =>
        new(grant.Id, grant.Reference, grant.OrganisationName, grant.ApplicationDate, grant.Status,
            grant.Tasks.Select(t => TaskResponse.From(t, today)).ToList());
}
