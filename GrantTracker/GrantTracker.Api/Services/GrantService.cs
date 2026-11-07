using GrantTracker.Api.Contracts;
using GrantTracker.Data;
using GrantTracker.Domain;
using GrantTracker.Domain.Entities;
using GrantTracker.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GrantTracker.Api.Services;

/// <summary>
/// Orchestration only: load the aggregate, call the domain method that owns the rule, save, map.
/// Business rules live in <see cref="Grant"/>; this class never decides them.
/// </summary>
public sealed class GrantService(GrantTrackerDbContext db, TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<GrantSummaryResponse>> ListAsync(GrantStatus? status, CancellationToken ct)
    {
        IQueryable<Grant> grants = db.Grants.AsNoTracking();
        if (status is { } wanted)
        {
            grants = grants.Where(g => g.Status == wanted);
        }

        return await grants
            .OrderBy(g => g.Reference)
            .Select(g => new GrantSummaryResponse(
                g.Id, g.Reference, g.OrganisationName, g.ApplicationDate, g.Status,
                g.Tasks.Count(t => t.CompletedOn == null)))
            .ToListAsync(ct);
    }

    public async Task<GrantDetailResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var grant = await db.Grants
                        .AsNoTracking()
                        .Include(g => g.Tasks.OrderBy(t => t.DueDate))
                        .SingleOrDefaultAsync(g => g.Id == id, ct)
                    ?? throw new NotFoundException("Grant", id);

        return GrantDetailResponse.From(grant, timeProvider.Today());
    }

    public async Task<TaskResponse> AddTaskAsync(Guid grantId, CreateTaskRequest request, CancellationToken ct)
    {
        // Existing tasks are not needed to add one; AddTask only reads the grant itself.
        var grant = await db.Grants.SingleOrDefaultAsync(g => g.Id == grantId, ct)
                    ?? throw new NotFoundException("Grant", grantId);

        var now = timeProvider.GetLocalNow();
        var task = grant.AddTask(request.Title, request.Priority!.Value, request.DueDate!.Value, now);

        // Explicit Add: the domain assigns the task Id, and EF treats a keyed entity discovered through a
        // navigation as existing (it issued an UPDATE that matched no rows). Add makes it an INSERT.
        db.Tasks.Add(task);

        // One transaction: INSERT the task and UPDATE Grant.Version with the concurrency check.
        await db.SaveChangesAsync(ct);
        return TaskResponse.From(task, DateOnly.FromDateTime(now.DateTime));
    }

    public async Task<GrantSummaryResponse> CloseAsync(Guid id, CancellationToken ct)
    {
        // Close needs the task list to evaluate BR-5.
        var grant = await db.Grants
                        .Include(g => g.Tasks)
                        .SingleOrDefaultAsync(g => g.Id == id, ct)
                    ?? throw new NotFoundException("Grant", id);

        grant.Close();
        await db.SaveChangesAsync(ct);
        return GrantSummaryResponse.From(grant);
    }
}
