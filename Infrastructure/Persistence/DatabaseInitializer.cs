// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Data.Seed;
using Klacks.Api.Infrastructure.Persistence.StoredProcedures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Klacks.Api.Infrastructure.Persistence;

public interface IDatabaseInitializer
{
    Task InitializeAsync();

    Task SeedDataAsync();
}

public class DatabaseInitializer : IDatabaseInitializer
{
    private readonly DataBaseContext _context;
    private readonly ILogger<DatabaseInitializer> _logger;
    private readonly IConfiguration _configuration;
    private readonly IStoredProcedureInitializer _storedProcedureInitializer;

    public DatabaseInitializer(
        DataBaseContext context,
        ILogger<DatabaseInitializer> logger,
        IConfiguration configuration,
        IStoredProcedureInitializer storedProcedureInitializer)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
        _storedProcedureInitializer = storedProcedureInitializer;
    }

    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Starting database initialization...");

            // Check if database exists, create if not
            try
            {
                await _context.Database.OpenConnectionAsync();
                await _context.Database.CloseConnectionAsync();
            }
            catch (Npgsql.PostgresException ex) when (ex.SqlState == "3D000") // database does not exist
            {
                _logger.LogInformation("Database does not exist. Creating database...");

                // Create database using master connection
                var connectionString = _context.Database.GetConnectionString();
                var builder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
                var databaseName = builder.Database;
                builder.Database = "postgres"; // Connect to default postgres database

                using var masterConnection = new Npgsql.NpgsqlConnection(builder.ToString());
                await masterConnection.OpenAsync();
                using var command = masterConnection.CreateCommand();
                command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
                await command.ExecuteNonQueryAsync();

                _logger.LogInformation("Database '{DatabaseName}' created successfully.", databaseName);
            }

            // Execute migrations (ignore empty migrations)
            try
            {
                if ((await _context.Database.GetPendingMigrationsAsync()).Any())
                {
                    _logger.LogInformation("Executing pending migrations...");
                    await _context.Database.MigrateAsync();
                }
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("PendingModelChangesWarning"))
            {
                _logger.LogWarning("Model changes detected but no migration needed. Forcing migration to ensure __EFMigrationsHistory table...");

                var appliedMigrations = await _context.Database.GetAppliedMigrationsAsync();
                if (!appliedMigrations.Any())
                {
                    _logger.LogInformation("No migrations applied. Forcing migration to create __EFMigrationsHistory table...");
                    await _context.Database.MigrateAsync();
                }
            }

            await _storedProcedureInitializer.InitializeAsync();

            await SeedDataAsync();

            _logger.LogInformation("Database initialization completed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database initialization");
            throw;
        }
    }

    public async Task SeedDataAsync()
    {
        if (await _context.Client.AnyAsync())
        {
            _logger.LogInformation("Database already contains data. Skipping seed.");
            return;
        }

        _logger.LogInformation("Starting seed data insertion...");

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var activeProvider = _context.Database.ProviderName;
            var migrationBuilder = new MigrationBuilder(activeProvider);

            var withFake = _configuration.GetValue<bool>("Fake:WithFake", false);

            DataSeeder.Add(migrationBuilder, withFake);

            var sqlOperations = migrationBuilder.Operations.OfType<SqlOperation>();

            foreach (var operation in sqlOperations)
            {
                if (!string.IsNullOrWhiteSpace(operation.Sql))
                {
                    _logger.LogDebug("Executing SQL: {SqlPreview}...", operation.Sql.Substring(0, Math.Min(100, operation.Sql.Length)));
                    await _context.Database.ExecuteSqlRawAsync(operation.Sql);
                }
            }

            await transaction.CommitAsync();

            var clientCount = await _context.Client.CountAsync();
            _logger.LogInformation("Seed data successfully inserted. {ClientCount} clients created.", clientCount);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error inserting seed data");
            throw;
        }
    }
}

public static class DatabaseInitializerExtensions
{
    public static IServiceCollection AddDatabaseInitializer(this IServiceCollection services)
    {
        services.AddStoredProcedureInitializer();
        services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();
        return services;
    }
}