using Microsoft.EntityFrameworkCore;

namespace GrantTracker.Data;

/// <summary>
/// Creates the schema and seeds it at start-up. <paramref name="recreate"/> drops the database first,
/// which the Development profile uses so seed dates are always relative to the day the app starts.
/// Before running for real: replace EnsureCreated with EF migrations and never recreate (see README).
/// </summary>
public static class DatabaseInitializer
{
    public static void Initialize(GrantTrackerDbContext db, TimeProvider timeProvider, bool recreate)
    {
        if (recreate)
        {
            db.Database.EnsureDeleted();
        }

        db.Database.EnsureCreated();
        DatabaseSeeder.Seed(db, timeProvider);
    }
}
