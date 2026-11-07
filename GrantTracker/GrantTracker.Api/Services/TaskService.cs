using GrantTracker.Api.Contracts;
using GrantTracker.Data;
using GrantTracker.Domain;
using GrantTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GrantTracker.Api.Services;

public sealed class TaskService(GrantTrackerDbContext db, TimeProvider timeProvider)
{
    public async Task<TaskResponse> CompleteAsync(Guid id, CancellationToken ct)
    {
        var task = await db.Tasks.SingleOrDefaultAsync(t => t.Id == id, ct)
                   ?? throw new NotFoundException("Task", id);

        var today = timeProvider.Today();
        task.Complete(today);
        await db.SaveChangesAsync(ct);
        return TaskResponse.From(task, today);
    }

    public async Task<IReadOnlyList<OverdueTaskResponse>> OverdueAsync(CancellationToken ct)
    {
        var today = timeProvider.Today();
        var overdue = await db.Tasks
            .AsNoTracking()
            .Include(t => t.Grant)
            .Where(GrantTask.OverdueOn(today))
            .OrderBy(t => t.DueDate)
            .ThenBy(t => t.Grant.Reference)
            .ThenBy(t => t.Title)
            .ToListAsync(ct);

        return overdue.Select(t => OverdueTaskResponse.From(t, today)).ToList();
    }
}
