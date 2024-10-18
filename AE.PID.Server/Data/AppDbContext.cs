using Microsoft.EntityFrameworkCore;

namespace AE.PID.Server.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppVersion> AppVersions { get; set; }
    public DbSet<Library> Libraries { get; set; }
    public DbSet<RepositorySnapshot> RepositorySnapshots { get; set; }
    public DbSet<LibraryVersion> LibraryVersions { get; set; }

    public DbSet<Stencil> Stencils { get; set; }
    public DbSet<Master> Masters { get; set; }
    public DbSet<MasterContentSnapshot> MasterContentSnapshots { get; set; }
    public DbSet<StencilSnapshot> StencilSnapshots { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<EntityBase>();

        // modelBuilder.Entity<AppVersion>()
        //     .ToTable("AppVersions"); 
        // modelBuilder.Entity<Library>()
        //     .ToTable("Libraries"); 
        // modelBuilder.Entity<RepositorySnapshot>()
        //     .ToTable("LibrarySnapshots"); 
        // modelBuilder.Entity<LibraryVersion>()
        //     .ToTable("LibraryVersions"); 
        // modelBuilder.Entity<LibraryItem>()
        //     .ToTable("LibraryVersionItems"); 
        // modelBuilder.Entity<LibraryVersionItemXML>()
        //     .ToTable("LibraryVersionItemXMLs"); 

        // base.OnModelCreating(modelBuilder);

        // // treat as abstract class
        // modelBuilder.Ignore<EntityBase>();
        // modelBuilder.Entity<EntityBase>()
        //     .Property(b => b.CreatedAt)
        //     .HasDefaultValueSql("CURRENT_TIMESTAMP"); // 设置 SQLite 的默认值为当前时间

        // modelBuilder.Entity<LibraryEntity>()
        //     .HasMany(e => e.Versions)
        //     .WithOne(e => e.Library)
        //     .HasForeignKey(e => e.LibraryId)
        //     .IsRequired();
        // modelBuilder.Entity<LibraryVersionEntity>()
        //     .HasMany(e => e.Items)
        //     .WithOne(e => e.Version)
        //     .HasForeignKey(e => e.VersionId)
        //     .IsRequired();
        //
        // modelBuilder.Entity<VersionHashEntity>()
        //     .HasMany(e => e.Versions);
    }

    // public override int SaveChanges()
    // {
    //     foreach (var entry in ChangeTracker.Entries()
    //                  .Where(e => e.State == EntityState.Modified))
    //     {
    //         // 设置 ModifiedAt 为当前时间
    //         if (entry.Property("ModifiedAt") != null)
    //         {
    //             entry.Property("ModifiedAt").CurrentValue = DateTime.UtcNow;
    //         }
    //     }
    //
    //     return base.SaveChanges();
    // }
}