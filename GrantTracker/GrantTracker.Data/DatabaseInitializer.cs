using Microsoft.EntityFrameworkCore;

namespace GrantTracker.Data;

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
