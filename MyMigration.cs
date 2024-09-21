using Klacks.Api.Datas;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api;

public class MyMigration
{

public MyMigration(IConfiguration configuration, ILoggerFactory LoggerFactory)
{

  var connectionString = configuration.GetConnectionString("Default");
  var DatabaseContextOptionsBuilder = new DbContextOptionsBuilder<DataBaseContext>();
  DatabaseContextOptionsBuilder.UseNpgsql(connectionString);
  DatabaseContextOptionsBuilder.EnableDetailedErrors();
  DatabaseContextOptionsBuilder.EnableSensitiveDataLogging();
  DatabaseContextOptionsBuilder.UseLoggerFactory(LoggerFactory);

  using var context = new DataBaseContext(DatabaseContextOptionsBuilder.Options, null!);
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
