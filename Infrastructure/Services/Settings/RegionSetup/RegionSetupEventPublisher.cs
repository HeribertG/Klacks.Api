// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Post-commit hooks of the region setup: raises SchedulingRuleRateRevisionsImportedEvent when rate
/// revisions or rule surcharge fields actually changed and SurchargeSettingsChangedEvent when
/// surcharge-relevant global settings actually changed - never on a no-op import. Fully defensive: the
/// import is already committed, so a failing recalculation hook is logged and never breaks startup.
/// </summary>
/// <param name="eventDispatcher">Dispatches the domain events to their handlers</param>
/// <param name="logger">Logger instance for diagnostic output</param>

using Klacks.Api.Domain.Events;

namespace Klacks.Api.Infrastructure.Services.Settings.RegionSetup;

public sealed class RegionSetupEventPublisher
{
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly ILogger<RegionSetupEventPublisher> _logger;

    public RegionSetupEventPublisher(IDomainEventDispatcher eventDispatcher, ILogger<RegionSetupEventPublisher> logger)
    {
        _eventDispatcher = eventDispatcher;
        _logger = logger;
    }

    public async Task DispatchRateRevisionsImportedAsync(
        IReadOnlyList<RateRevisionChange> rateRevisionChanges,
        IReadOnlyList<Guid> changedSurchargeRuleIds)
    {
        if (rateRevisionChanges.Count == 0 && changedSurchargeRuleIds.Count == 0)
        {
            return;
        }

        try
        {
            var ruleIds = rateRevisionChanges.Select(c => c.RuleId)
                .Concat(changedSurchargeRuleIds)
                .Distinct()
                .ToList();
            DateOnly? earliestChangedValidFrom = rateRevisionChanges.Count > 0
                ? rateRevisionChanges.Min(c => c.ValidFrom)
                : null;

            await _eventDispatcher.DispatchAsync(
                new SchedulingRuleRateRevisionsImportedEvent(ruleIds, earliestChangedValidFrom),
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Post-commit dispatch of {EventName} failed after region setup import; the import is persisted and remains unaffected.",
                nameof(SchedulingRuleRateRevisionsImportedEvent));
        }
    }

    public async Task DispatchSurchargeSettingsChangedAsync(IReadOnlyList<string> changedKeys)
    {
        if (changedKeys.Count == 0)
        {
            return;
        }

        try
        {
            await _eventDispatcher.DispatchAsync(
                new SurchargeSettingsChangedEvent(changedKeys),
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Post-commit dispatch of {EventName} failed after region setup import; the settings are persisted and remain unaffected.",
                nameof(SurchargeSettingsChangedEvent));
        }
    }
}
