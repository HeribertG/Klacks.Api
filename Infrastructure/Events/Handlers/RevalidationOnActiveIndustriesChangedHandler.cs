// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reacts to a committed ACTIVE_INDUSTRIES change. The setting itself only filters selection lists,
/// so before this handler existed a switch changed nothing observable at all: no rule was reassigned,
/// no schedule was re-judged, and the error list kept showing the previous state until someone
/// happened to change the visible period.
/// It re-validates the unlocked real-mode work window so the error list reflects the new state, and
/// logs how many contracts still reference a rule of a now-inactive industry. Those assignments are
/// deliberately left untouched - contracts are legal documents and are migrated by the admin through
/// the migration list, never rewritten automatically.
/// Runs post-commit and fully defensive: a failure is logged and never affects the committed change.
/// </summary>
/// <param name="migrationReader">Counts the contracts left on inactive industries</param>
/// <param name="scope">Resolves the unlocked real-mode work window to re-validate</param>
/// <param name="timelineService">Queues the range check that refreshes the error list</param>
/// <param name="logger">Records the outcome of the switch</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Scheduling;
using Klacks.Api.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.Events.Handlers;

public sealed class RevalidationOnActiveIndustriesChangedHandler : IDomainEventHandler<ActiveIndustriesChangedEvent>
{
    private readonly IIndustryMigrationReader _migrationReader;
    private readonly ISurchargeRecalculationScope _scope;
    private readonly IScheduleTimelineService _timelineService;
    private readonly ILogger<RevalidationOnActiveIndustriesChangedHandler> _logger;

    public RevalidationOnActiveIndustriesChangedHandler(
        IIndustryMigrationReader migrationReader,
        ISurchargeRecalculationScope scope,
        IScheduleTimelineService timelineService,
        ILogger<RevalidationOnActiveIndustriesChangedHandler> logger)
    {
        _migrationReader = migrationReader;
        _scope = scope;
        _timelineService = timelineService;
        _logger = logger;
    }

    public async Task HandleAsync(ActiveIndustriesChangedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            var candidates = await _migrationReader.GetContractsOnInactiveIndustriesAsync(cancellationToken);
            if (candidates.Count > 0)
            {
                _logger.LogInformation(
                    "Active industries changed from {PreviousValue} to {CurrentValue}: {ContractCount} contract(s) covering {ClientCount} client(s) still reference a scheduling rule of an inactive industry and await migration.",
                    domainEvent.PreviousValue ?? string.Empty,
                    domainEvent.CurrentValue,
                    candidates.Count,
                    candidates.Sum(c => c.AffectedClientCount));
            }

            var window = await _scope.GetUnlockedRealWorkWindowAsync(cancellationToken);
            if (window == null)
            {
                _logger.LogDebug("Active industries changed, but no unlocked real-mode work exists to re-validate.");
                return;
            }

            _timelineService.QueueRangeCheck(window.From, window.Until, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Post-commit handling of {EventName} failed; the settings update is persisted and remains unaffected.",
                nameof(ActiveIndustriesChangedEvent));
        }
    }
}
