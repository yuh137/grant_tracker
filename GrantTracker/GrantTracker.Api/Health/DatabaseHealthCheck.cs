using GrantTracker.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GrantTracker.Api.Health;

public class DatabaseHealthCheck(GrantTrackerDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            await db.Grants.AsNoTracking().AnyAsync(ct);
            return HealthCheckResult.Healthy("The grants table answered a query.");
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "The database did not answer.", ex);
        }
    }
}
