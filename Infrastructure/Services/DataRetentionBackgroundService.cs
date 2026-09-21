// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Background service that permanently deletes soft-deleted records after the configured retention period.
/// Runs daily and uses raw SQL to bypass EF Core query filters and soft-delete interception.
/// Each table is purged in isolation: a row still referenced by a restricted foreign key (for example
/// agent_conditions.caused_by_condition_id) makes its own DELETE fail, and without the per-table guard
/// that single failure would abort every table queued behind it, so the purge silently stopped halfway
/// and retention was never enforced for the rest of the model.
/// </summary>
/// <param name="scopeFactory">Factory for creating DI scopes in background tasks</param>

using SettingsConstants = Klacks.Api.Application.Constants.Settings;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services;

public class DataRetentionBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DataRetentionBackgroundService> _logger;

    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(10);

    public DataRetentionBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<DataRetentionBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Data retention background service started");

        try
        {
            await Task.Delay(InitialDelay, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PurgeExpiredRecordsAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in data retention background service");
                }

                await Task.Delay(Interval, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }

        _logger.LogInformation("Data retention background service stopped");
    }

    private async Task PurgeExpiredRecordsAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataBaseContext>();

        var retentionDays = await GetRetentionDaysAsync(context, stoppingToken);
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

        _logger.LogInformation(
            "Running data retention cleanup (retention: {Days} days, cutoff: {Cutoff})",
            retentionDays,
            cutoffDate);

        await SoftDeleteExpiredLedgerRowsAsync(scope.ServiceProvider, stoppingToken);

        var totalDeleted = 0;

        var entityTypes = context.Model.GetEntityTypes()
            .Where(e => typeof(BaseEntity).IsAssignableFrom(e.ClrType))
            .ToList();

        foreach (var entityType in entityTypes)
        {
            var tableName = entityType.GetTableName();
            var schema = entityType.GetSchema() ?? "public";

            if (string.IsNullOrEmpty(tableName))
            {
                continue;
            }

            var isDeletedColumn = entityType.FindProperty(nameof(BaseEntity.IsDeleted))?.GetColumnName();
            var deletedTimeColumn = entityType.FindProperty(nameof(BaseEntity.DeletedTime))?.GetColumnName();

            if (string.IsNullOrEmpty(isDeletedColumn) || string.IsNullOrEmpty(deletedTimeColumn))
            {
                continue;
            }

            var sql = "DELETE FROM \"" + schema + "\".\"" + tableName + "\" WHERE \"" + isDeletedColumn + "\" = true AND \"" + deletedTimeColumn + "\" < {0}";

            try
            {
                var deleted = await context.Database.ExecuteSqlRawAsync(
                    sql,
                    [cutoffDate],
                    stoppingToken);

                if (deleted > 0)
                {
                    _logger.LogInformation(
                        "Permanently deleted {Count} records from {Table}",
                        deleted,
                        tableName);
                    totalDeleted += deleted;
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(
                    ex,
                    "Data retention purge failed for {Table}; continuing with the remaining tables",
                    tableName);
            }
        }

        _logger.LogInformation("Data retention cleanup completed: {Total} records permanently deleted", totalDeleted);
    }

    private async Task SoftDeleteExpiredLedgerRowsAsync(IServiceProvider services, CancellationToken stoppingToken)
    {
        try
        {
            var nowUtc = services.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;
            var conditionRepository = services.GetRequiredService<IAgentConditionRepository>();
            var dispatchRepository = services.GetRequiredService<IProactiveTriggerDispatchRepository>();

            var (conditions, events) = await conditionRepository.SoftDeleteExpiredAsync(
                AgentLedgerRetentionPolicy.ShortLivedConditionCutoff(nowUtc),
                AgentLedgerRetentionPolicy.LongLivedConditionCutoff(nowUtc),
                nowUtc,
                stoppingToken);
            var dispatches = await dispatchRepository.SoftDeleteExpiredAsync(
                AgentLedgerRetentionPolicy.DispatchCutoff(nowUtc),
                nowUtc,
                stoppingToken);

            if (conditions > 0 || events > 0 || dispatches > 0)
            {
                _logger.LogInformation(
                    "Ledger retention soft-deleted {Conditions} condition(s), {Events} event(s), {Dispatches} dispatch row(s)",
                    conditions,
                    events,
                    dispatches);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ledger retention soft-delete failed; continuing with the physical purge");
        }
    }

    private static async Task<int> GetRetentionDaysAsync(DataBaseContext context, CancellationToken stoppingToken)
    {
        var setting = await context.Settings
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.Type == SettingsConstants.DATA_RETENTION_DAYS,
                stoppingToken);

        if (setting is not null && int.TryParse(setting.Value, out var days) && days > 0)
        {
            return days;
        }

        return SettingsConstants.DATA_RETENTION_DAYS_DEFAULT;
    }
}
