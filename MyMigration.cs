using Klacks.Api.Datas;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api;

public class MyMigration
{
    public MyMigration(IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        var connectionString = configuration.GetConnectionString("Default");
        var databaseContextOptionsBuilder = new DbContextOptionsBuilder<DataBaseContext>();
        databaseContextOptionsBuilder.UseNpgsql(connectionString);
        databaseContextOptionsBuilder.EnableDetailedErrors();
        databaseContextOptionsBuilder.EnableSensitiveDataLogging();
        databaseContextOptionsBuilder.UseLoggerFactory(loggerFactory);

        using var context = new DataBaseContext(databaseContextOptionsBuilder.Options, null!);
        var pendingMigrations = context.Database.GetPendingMigrations().ToArray();
        if (pendingMigrations.Any())
        {
            foreach (var pendingMigration in pendingMigrations)
            {
                Console.WriteLine("Applying migrations...");
                Console.WriteLine("\t" + pendingMigration);
                Console.WriteLine("DONE");
            }

            context.Database.Migrate();
        }
    }
}
