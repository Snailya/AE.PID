using AE.PID.Core;
using AE.PID.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace AE.PID.Server;

public static class MigrationFix
{
    public static void ApplyMigration(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();

        // 2025.04.09：为了保证查询语句可以在数据库中执行，而不是在客户都安执行，对Version进行了拆表，因此需要对历史数据进行填充。
        Fix_VersionChannelSupport(db);
        Fix_AddVersionComponents(db);
    }

    private static void Fix_AddVersionComponents(AppDbContext context)
    {
        var entities = context.AppVersions.ToList();
        foreach (var entity in entities)
            if (!string.IsNullOrEmpty(entity.Version))
            {
                var parts = entity.Version.Split('.');
                entity.Major = int.Parse(parts[0]);
                entity.Minor = parts.Length > 1 ? int.Parse(parts[1]) : 0;
                entity.Build = parts.Length > 2 ? int.Parse(parts[2]) : 0;
                entity.Revision = parts.Length > 3 ? int.Parse(parts[3]) : 0;

                context.AppVersions.Update(entity);
            }

        context.SaveChanges();
    }

    private static void Fix_VersionChannelSupport(AppDbContext context)
    {
        var entities = context.AppVersions.ToList();
        foreach (var entity in entities.Where(entity => entity.Channel == 0))
        {
            entity.Channel = VersionChannel.GeneralAvailability;
            context.AppVersions.Update(entity);
        }

        context.SaveChanges();
    }
}