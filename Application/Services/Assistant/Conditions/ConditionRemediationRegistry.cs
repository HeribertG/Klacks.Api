// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default <see cref="IConditionRemediationRegistry"/>. One entry as of Etappe 5b: empty_container is
/// remediated by create_container_template.
///
/// History that explains the shape of this class, because the reasoning is easy to lose: until Etappe
/// 4b it was deliberately EMPTY. The three Etappe-2 kinds (open_order, empty_container,
/// uncut_fullday_shift) are structural Shift operations (sealing, container-template creation, cutting),
/// and none of them can travel through the AnalyseScenario mechanism -
/// AcceptAnalyseScenarioCommandHandler.PromoteScenarioWorksAsync only ever promotes Work, WorkChange and
/// Expenses rows back out of a scenario, never Shift structure, status transitions or newly created
/// Shifts (AnalyseScenarioService.cs:336-390). So all three became Execute-only, and with no execution
/// stage in existence, an entry would have granted a permission nothing could use.
///
/// Etappe 5b built that execution stage, so the rule "an entry only once a remediation is genuinely
/// SCENARIO-capable" no longer holds and has been replaced by the honest one: an entry means a
/// remediation EXISTS, and <see cref="ConditionRemediationEntry.IsScenarioCapable"/> says whether it can
/// also be prepared rather than only executed. A kind still absent from here stays capped at Hint by
/// <see cref="TryGetEffectiveMaxAction"/> regardless of its configured MaxAction or any Etappe-4e
/// delegation - that is the second, code-only security gate this class exists to be, and only a
/// reviewed code change can open it.
/// </summary>
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Conditions;

public sealed class ConditionRemediationRegistry : IConditionRemediationRegistry
{
    private static readonly IReadOnlyDictionary<string, ConditionRemediationEntry> Entries =
        new Dictionary<string, ConditionRemediationEntry>(StringComparer.Ordinal)
        {
            [AgentTriggerKinds.EmptyContainer] = new ConditionRemediationEntry(
                CreateContainerTemplateParameters.SkillName,
                new EmptyContainerRemediationBinder(),
                CreateContainerTemplateParameters.Required,
                IsScenarioCapable: false)
        };

    private static readonly IReadOnlyCollection<string> KindsWithRemediation = Entries.Keys.ToArray();

    public IReadOnlyCollection<string> RegisteredKinds => KindsWithRemediation;

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
