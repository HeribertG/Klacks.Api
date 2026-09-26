// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Runs the autonomous period close once per trigger tick and hands its outcomes (closed / blocked) to the
/// tick, which records them in the condition ledger and notifies the planners. All decisions live in
/// IPeriodAutoCloseService; this class only binds it to the tick under the period_auto_close kind.
///
/// Also an IAgentConditionFingerprintSource, so the outcome rows do not stay open for ever: the active set is
/// exactly the fingerprints of the events this instance's DetectAsync returned in the same tick (the tick calls
/// both on the same scoped instance, DetectAsync first). A blocked cause that still holds is emitted again, keeps
/// its fingerprint and therefore its row - the recipient dedup on that row suppresses a repeat notification.
/// A cause that is gone (lag stored, errors fixed, period closed by a person) and a "closed" report from the
/// previous tick are not emitted again and resolve, which also ends their reminders. The evaluation cannot be
/// repeated here to build the set, because it seals periods; when GetActiveFingerprintsAsync is called without a
/// preceding DetectAsync on this instance it therefore returns the fingerprints of all open rows of the kind and
/// resolves nothing - the safe direction.
/// </summary>
/// <param name="autoCloseService">The autonomous period close.</param>
/// <param name="conditionRepository">Open rows of the kind, for the no-DetectAsync fallback.</param>

using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed class PeriodAutoCloseDetector : IAgentTriggerDetector, IAgentConditionFingerprintSource
{
    private readonly IPeriodAutoCloseService _autoCloseService;
    private readonly IAgentConditionRepository _conditionRepository;
    private IReadOnlySet<string>? _emittedFingerprints;

    public PeriodAutoCloseDetector(
        IPeriodAutoCloseService autoCloseService,
        IAgentConditionRepository conditionRepository)
    {
        _autoCloseService = autoCloseService;
        _conditionRepository = conditionRepository;
    }

    public string Kind => AgentTriggerKinds.PeriodAutoClose;

    public async Task<IReadOnlyList<IAgentTriggerEvent>> DetectAsync(CancellationToken cancellationToken = default)
    {
        var events = await _autoCloseService.RunAsync(cancellationToken);
        _emittedFingerprints = events
            .Select(AgentConditionLedgerPolicy.FingerprintFor)
            .ToHashSet(StringComparer.Ordinal);

        return events;
    }

    public async Task<IReadOnlySet<string>> GetActiveFingerprintsAsync(CancellationToken cancellationToken = default)
    {
        if (_emittedFingerprints != null)
        {
            return _emittedFingerprints;
        }

        var openRows = await _conditionRepository.GetOpenByKindAsync(Kind, cancellationToken);
        return openRows.Select(row => row.Fingerprint).ToHashSet(StringComparer.Ordinal);
    }
}
