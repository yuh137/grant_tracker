using GrantTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GrantTracker.Data;

public class GrantTrackerDbContext(DbContextOptions<GrantTrackerDbContext> options) : DbContext(options)
{
    public DbSet<Grant> Grants => Set<Grant>();
    public DbSet<GrantTask> Tasks => Set<GrantTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Grant>(grant =>
        {
            grant.HasKey(g => g.Id);
            grant.Property(g => g.Reference).HasMaxLength(20).IsRequired();
            grant.HasIndex(g => g.Reference).IsUnique();
            grant.Property(g => g.OrganisationName).HasMaxLength(200).IsRequired();

            grant.Property(g => g.Status).HasConversion<string>().HasMaxLength(10);

            grant.Property(g => g.Version).IsConcurrencyToken();

            grant.HasMany(g => g.Tasks)
                .WithOne(t => t.Grant)
                .HasForeignKey(t => t.GrantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GrantTask>(task =>
        {
            task.HasKey(t => t.Id);
            task.Property(t => t.Title).HasMaxLength(GrantTask.MaxTitleLength).IsRequired();
            task.Property(t => t.Priority).HasConversion<string>().HasMaxLength(10);

            task.HasIndex(t => new { t.CompletedOn, t.DueDate });
        });
    }
}
