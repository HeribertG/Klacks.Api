// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Weekly trend logger for the autonomy-roadmap S10 coverage metric. Reads
/// docs/klacksy-usecases.md once per week (first run 10 minutes after start) and writes a
/// structured log line so observability can chart coverage over time. Persisted eval_runs
/// integration is the next iteration's job — for now the log line is the trend backbone.
/// </summary>
/// <param name="scopeFactory">Creates a scoped DI provider per tick.</param>
/// <param name="logger">Structured log emits the report.</param>

using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public class SkillCoverageBackgroundService : BackgroundService
{
    private const int FirstRunDelayMinutes = 10;
    private static readonly TimeSpan ScanInterval = TimeSpan.FromDays(7);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SkillCoverageBackgroundService> _logger;

    public SkillCoverageBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<SkillCoverageBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Skill coverage background service started");

        try
        {
            await Task.Delay(TimeSpan.FromMinutes(FirstRunDelayMinutes), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await EmitTrendAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Skill coverage tick failed");
                }

                await Task.Delay(ScanInterval, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }

        _logger.LogInformation("Skill coverage background service stopped");
    }

    private async Task EmitTrendAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var coverage = scope.ServiceProvider.GetRequiredService<ISkillCoverageService>();
        var report = await coverage.ComputeAsync(cancellationToken);

        _logger.LogInformation(
            "Coverage trend: {Percent}% ({Covered}/{Total} covered, {Partial} partial, {Missing} missing) @ {Timestamp:O}",
            report.CoveragePercent, report.Covered, report.Total, report.Partial, report.Missing, report.ComputedAtUtc);
    }
}
