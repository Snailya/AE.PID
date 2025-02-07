using AE.PID.Server.Data.Recommendation;
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

    public DbSet<UserMaterialSelection> UserMaterialSelections { get; set; }
    public DbSet<MaterialRecommendationCollection> MaterialRecommendationCollections { get; set; }
    public DbSet<MaterialRecommendationCollectionFeedback> MaterialRecommendationCollectionFeedbacks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<EntityBase>();
        //     modelBuilder.Entity<UserMaterialSelection>().ComplexProperty(u => u.Context);
        //     modelBuilder.Entity<MaterialRecommendationCollection>().ComplexProperty(u => u.Context);
    }
}