// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Persistence.StoredProcedures;

public interface IStoredProcedureInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}

public class StoredProcedureInitializer : IStoredProcedureInitializer
{
    private readonly DataBaseContext _context;
    private readonly ILogger<StoredProcedureInitializer> _logger;

    public StoredProcedureInitializer(
        DataBaseContext context,
        ILogger<StoredProcedureInitializer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing stored procedures...");

        var sqlFiles = GetEmbeddedSqlFiles();

        foreach (var (fileName, sql) in sqlFiles)
        {
            try
            {
                _logger.LogDebug("Executing stored procedure: {FileName}", fileName);
                await _context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
                _logger.LogInformation("Stored procedure '{FileName}' created/updated successfully", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating stored procedure '{FileName}'", fileName);
                throw;
            }
        }

        _logger.LogInformation("Stored procedures initialization completed. {Count} procedures processed", sqlFiles.Count);
    }

    private List<(string FileName, string Sql)> GetEmbeddedSqlFiles()
    {
        var result = new List<(string, string)>();
        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames()
            .Where(name => name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var resourceName in resourceNames)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                _logger.LogWarning("Could not load embedded resource: {ResourceName}", resourceName);
                continue;
            }

            using var reader = new StreamReader(stream);
            var sql = reader.ReadToEnd();
            var fileName = resourceName.Split('.').TakeLast(2).First() + ".sql";
            result.Add((fileName, sql));
        }

        return result;
    }
}

public static class StoredProcedureInitializerExtensions
{
    public static IServiceCollection AddStoredProcedureInitializer(this IServiceCollection services)
    {
        services.AddScoped<IStoredProcedureInitializer, StoredProcedureInitializer>();
        return services;
    }
}
