// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Creates the database if missing, applies migrations and runs the one-time system seed;
/// demo data is only included when the region setup profile or the legacy fake configuration requests it.
/// </summary>
/// <param name="context">EF Core database context used for migrations and seeding</param>
/// <param name="configuration">App configuration providing connection string, region setup file path and legacy fake flag</param>
/// <param name="storedProcedureInitializer">Installs or refreshes the stored procedures</param>
/// <param name="identityProviderSecretBackfill">Encrypts legacy identity provider secrets</param>
/// <param name="demoOrderSeedFileWriter">Exports the demo shift plan as an ERP order file when demo shifts are not seeded</param>
/// <param name="logger">Logger instance for diagnostic output</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Data.Seed;
using Klacks.Api.Infrastructure.Persistence.Seed.Demo;
using Klacks.Api.Infrastructure.Persistence.StoredProcedures;
using Klacks.Api.Infrastructure.Services.Settings;
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
    private const string FakeWithFakeConfigKey = "Fake:WithFake";

    private readonly DataBaseContext _context;
    private readonly ILogger<DatabaseInitializer> _logger;
    private readonly IConfiguration _configuration;
    private readonly IStoredProcedureInitializer _storedProcedureInitializer;
    private readonly IIdentityProviderSecretBackfill _identityProviderSecretBackfill;
    private readonly IDemoOrderSeedFileWriter _demoOrderSeedFileWriter;

    public DatabaseInitializer(
        DataBaseContext context,
        ILogger<DatabaseInitializer> logger,
        IConfiguration configuration,
        IStoredProcedureInitializer storedProcedureInitializer,
        IIdentityProviderSecretBackfill identityProviderSecretBackfill,
        IDemoOrderSeedFileWriter demoOrderSeedFileWriter)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
        _storedProcedureInitializer = storedProcedureInitializer;
        _identityProviderSecretBackfill = identityProviderSecretBackfill;
        _demoOrderSeedFileWriter = demoOrderSeedFileWriter;
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

                var connectionString = _configuration.GetConnectionString("DefaultConnection")
                    ?? _context.Database.GetConnectionString();
                var builder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
                var databaseName = builder.Database;
                builder.Database = "postgres";
                _logger.LogInformation(
                    "Creating database via master connection: host={Host}, hasPassword={HasPassword}, fromConfiguration={FromConfiguration}",
                    builder.Host,
                    !string.IsNullOrEmpty(builder.Password),
                    _configuration.GetConnectionString("DefaultConnection") != null);

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

            await _identityProviderSecretBackfill.EncryptLegacySecretsAsync();

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
        if (await _context.Users.AnyAsync() || await _context.Client.AnyAsync())
        {
            _logger.LogInformation("Database already contains seeded system data (users or clients). Skipping seed.");
            return;
        }

        var (seedDemoData, seedDemoShiftsAndGroups, seedLanguage) = await ResolveDemoDataSeedAsync();

        _logger.LogInformation("Starting seed data insertion...");

        var previousTimeout = _context.Database.GetCommandTimeout();
        _context.Database.SetCommandTimeout(TimeSpan.FromMinutes(15));

        var strategy = _context.Database.CreateExecutionStrategy();

        try
        {
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var activeProvider = _context.Database.ProviderName;
                    var migrationBuilder = new MigrationBuilder(activeProvider);

                    DataSeeder.Add(migrationBuilder, seedDemoData, seedDemoShiftsAndGroups, seedLanguage);

                    var sqlOperations = migrationBuilder.Operations.OfType<SqlOperation>();

                    foreach (var operation in sqlOperations)
                    {
                        if (!string.IsNullOrWhiteSpace(operation.Sql))
                        {
                            _logger.LogDebug("Executing SQL: {SqlPreview}...", operation.Sql.Substring(0, Math.Min(100, operation.Sql.Length)));
                            var sql = operation.Sql.Replace("{", "{{").Replace("}", "}}");
                            await _context.Database.ExecuteSqlRawAsync(sql);
                        }
                    }

                    await transaction.CommitAsync();

                    var clientCount = await _context.Client.CountAsync();
                    _logger.LogInformation("Seed data successfully inserted. {ClientCount} clients created.", clientCount);
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inserting seed data");
            throw;
        }
        finally
        {
            _context.Database.SetCommandTimeout(previousTimeout);
        }

        if (DemoDataSeedDecision.ShouldExportDemoOrders(seedDemoData, seedDemoShiftsAndGroups))
        {
            await _demoOrderSeedFileWriter.WriteAsync(seedLanguage);
        }
    }

    private async Task<(bool SeedDemoData, bool SeedDemoShiftsAndGroups, string Language)> ResolveDemoDataSeedAsync()
    {
        var regionFilePath = RegionSetupFileReader.GetConfiguredPath(_configuration);
        bool? profileSeedDemoData = null;
        bool? profileSeedDemoShiftsAndGroups = null;
        var language = "de";

        if (regionFilePath != null)
        {
            var profile = await RegionSetupFileReader.ReadProfileAsync(regionFilePath);
            profileSeedDemoData = profile.SeedDemoData;
            profileSeedDemoShiftsAndGroups = profile.SeedDemoShiftsAndGroups;
            language = string.IsNullOrWhiteSpace(profile.Languages?.Default) ? "de" : profile.Languages!.Default!;
        }

        var legacyFakeConfigEnabled = _configuration.GetValue(FakeWithFakeConfigKey, false);

        var (seedDemoData, seedDemoShiftsAndGroups, source) = DemoDataSeedDecision.Decide(
            regionFilePath != null,
            profileSeedDemoData,
            profileSeedDemoShiftsAndGroups,
            legacyFakeConfigEnabled);

        _logger.LogInformation(
            "Demo data seeding {State} ({ShiftsAndGroupsState} groups/group items/shifts), decided by {Source}.",
            seedDemoData ? "enabled" : "disabled",
            seedDemoShiftsAndGroups ? "including" : "excluding",
            source);

        return (seedDemoData, seedDemoShiftsAndGroups, language);
    }
}

public static class DatabaseInitializerExtensions
{
    public static IServiceCollection AddDatabaseInitializer(this IServiceCollection services)
    {
        services.AddStoredProcedureInitializer();
        services.AddScoped<IIdentityProviderSecretBackfill, IdentityProviderSecretBackfill>();
        services.AddScoped<IDemoOrderXmlGenerator, DemoOrderXmlGenerator>();
        services.AddScoped<IDemoOrderSeedFileWriter, DemoOrderSeedFileWriter>();
        services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();
        return services;
    }
}