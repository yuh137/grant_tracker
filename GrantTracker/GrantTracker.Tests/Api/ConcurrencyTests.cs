using GrantTracker.Data;
using GrantTracker.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static GrantTracker.Tests.Api.ApiFactory;

namespace GrantTracker.Tests.Api;

public class ConcurrencyTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Close_WithStaleGrant_AfterAnotherOfficerAddedHighTask_IsRejected()
    {
        var grantId = DatabaseSeeder.GrantIds.Hillview;
        using var officerA = _factory.Services.CreateScope();
        using var officerB = _factory.Services.CreateScope();
        var dbA = officerA.ServiceProvider.GetRequiredService<GrantTrackerDbContext>();
        var dbB = officerB.ServiceProvider.GetRequiredService<GrantTrackerDbContext>();

        var grantSeenByA = await dbA.Grants.Include(g => g.Tasks).SingleAsync(g => g.Id == grantId);
        var grantSeenByB = await dbB.Grants.Include(g => g.Tasks).SingleAsync(g => g.Id == grantId);

        var task = grantSeenByA.AddTask("Urgent eligibility check", TaskPriority.High, Today.AddDays(1),
            _factory.Clock.GetLocalNow());
        dbA.Tasks.Add(task);
        await dbA.SaveChangesAsync();

        grantSeenByB.Close();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => dbB.SaveChangesAsync());

        using var verify = _factory.Services.CreateScope();
        var db = verify.ServiceProvider.GetRequiredService<GrantTrackerDbContext>();
        var stored = await db.Grants.AsNoTracking().SingleAsync(g => g.Id == grantId);
        Assert.Equal(GrantStatus.Open, stored.Status);
        Assert.Equal(1, stored.Version);
    }
}
