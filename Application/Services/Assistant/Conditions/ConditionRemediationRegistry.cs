// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default <see cref="IConditionRemediationRegistry"/>. Deliberately EMPTY as of Etappe 4b - not an
/// oversight, an Owner decision recorded 2026-08-25 (F2+F3, Fable-consulted): the three Etappe-2 kinds
/// (open_order, empty_container, uncut_fullday_shift) turned out to be structural Shift operations
/// (sealing, container-template creation, cutting), none of which the AnalyseScenario mechanism can
/// carry. AcceptAnalyseScenarioCommandHandler.PromoteScenarioWorksAsync only ever promotes Work,
/// WorkChange and Expenses rows back out of a scenario - never Shift structure changes, status
/// transitions or newly created Shifts (AnalyseScenarioService.cs:336-390) - because the scenario
/// mechanism is a planning sandbox for work assignments, not a structure-sandbox channel. So all three
/// kinds became Execute-only: no Prepare stage, no scenario to prepare, direct execution deferred to
/// Etappe 5's full autonomy gate. A trigger kind gets an entry here only once a future remediation is
/// genuinely scenario-capable; until then every governed kind is capped at Hint by
/// <see cref="TryGetEffectiveMaxAction"/> regardless of its configured MaxAction, which is the intended,
/// verifiable behaviour this class exists to enforce, not a placeholder waiting to be filled in.
/// </summary>
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Conditions;

public sealed class ConditionRemediationRegistry : IConditionRemediationRegistry
{
    private static readonly IReadOnlyDictionary<string, ConditionRemediationEntry> Entries =
        new Dictionary<string, ConditionRemediationEntry>(StringComparer.Ordinal);

    public bool TryGetEntry(string triggerKind, out ConditionRemediationEntry? entry) =>
        Entries.TryGetValue(triggerKind, out entry);

    public ProactiveMaxAction TryGetEffectiveMaxAction(string triggerKind, ProactiveMaxAction configuredMaxAction)
    {
        if (configuredMaxAction <= ProactiveMaxAction.Hint)
        {
            return configuredMaxAction;
        }

        return Entries.ContainsKey(triggerKind) ? configuredMaxAction : ProactiveMaxAction.Hint;
    }
}
