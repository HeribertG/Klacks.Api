// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reference skeleton for a country-pack payroll-export hook that reacts to a closed period.
/// It is a gated, inert placeholder: it does NOT produce a real payroll export. It only demonstrates
/// the extension point — gate on a feature flag, run non-blocking after the seal has been committed.
/// A real country pack would load the period's work/allowance data, format it via an IExportFormatter,
/// and drop the file to the payroll target (SFTP/HTTP). Add one implementation per target market, and ship a
/// Plugins/Features/{name}/manifest.json so the feature gate can resolve and be enabled per customer.
/// </summary>
/// <param name="featurePluginService">Reads whether the country pack is enabled (feature-plugin gate keyed by the pack name)</param>
/// <param name="logger">Logs the reference hook activity</param>

using Klacks.Api.Application.Interfaces.Plugins;
using Klacks.Api.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.Events.Handlers;

public sealed class PayrollExportOnPeriodClosedHandler : IDomainEventHandler<PeriodClosedEvent>
{
    public const string FeaturePluginName = "payroll-export-de";

    private readonly IFeaturePluginService _featurePluginService;
    private readonly ILogger<PayrollExportOnPeriodClosedHandler> _logger;

    public PayrollExportOnPeriodClosedHandler(
        IFeaturePluginService featurePluginService,
        ILogger<PayrollExportOnPeriodClosedHandler> logger)
    {
        _featurePluginService = featurePluginService;
        _logger = logger;
    }

    public Task HandleAsync(PeriodClosedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (!_featurePluginService.IsEnabled(FeaturePluginName))
        {
            return Task.CompletedTask;
        }

        _logger.LogInformation(
            "Payroll-export country pack '{Plugin}' would export period {Start}..{End} (group {GroupId}): {WorkCount} work, {BreakCount} break, {SealedDayCount} sealed days, sealed by {SealedBy}.",
            FeaturePluginName,
            domainEvent.StartDate,
            domainEvent.EndDate,
            domainEvent.GroupId,
            domainEvent.WorkCount,
            domainEvent.BreakCount,
            domainEvent.SealedDayCount,
            domainEvent.SealedBy);

        return Task.CompletedTask;
    }
}
