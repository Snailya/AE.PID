using Microsoft.EntityFrameworkCore;

namespace AE.PID.Server.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppVersionEntity> AppVersions { get; set; }
    public DbSet<LibraryEntity> Libraries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LibraryEntity>()
            .HasMany(e => e.Versions)
            .WithOne(e => e.Library)
            .HasForeignKey(e => e.LibraryId)
            .IsRequired();
        modelBuilder.Entity<LibraryVersionEntity>()
            .HasMany(e => e.Items)
            .WithOne(e => e.Version)
            .HasForeignKey(e => e.VersionId)
            .IsRequired();
    }
}