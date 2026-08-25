// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Changes how far Klacksy may act by itself for one trigger kind, or flips the global kill switch.
/// Listed in SkillRiskClassifier.SensitiveSkills, which is what keeps Klacksy from ever widening its
/// own mandate: UnattendedSkillPolicy denies every sensitive skill on the background paths, and the
/// autonomy gate asks a human on every level. Klacksy may propose a change, never apply one.
/// </summary>
/// <param name="mediator">Dispatches the governance command.</param>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("set_proactive_governance")]
public class SetProactiveGovernanceSkill : BaseSkillImplementation
{
    private const string TriggerKindParameter = "trigger_kind";
    private const string GroupIdParameter = "group_id";
    private const string MaxActionParameter = "max_action";
    private const string EnabledParameter = "enabled";
    private const string ResponsibleOwnerParameter = "responsible_owner_user_id";
    private const string ClearResponsibleOwnerParameter = "clear_responsible_owner";
    private const string DailyActionBudgetParameter = "daily_action_budget";
    private const string WindowActionLimitParameter = "window_action_limit";
    private const string WindowMinutesParameter = "window_minutes";
    private const string KillSwitchParameter = "kill_switch";

    private readonly IMediator _mediator;

    public SetProactiveGovernanceSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var maxActionText = GetParameter<string?>(parameters, MaxActionParameter);
        ProactiveMaxAction? maxAction = null;
        if (!string.IsNullOrWhiteSpace(maxActionText))
        {
            if (!Enum.TryParse<ProactiveMaxAction>(maxActionText, ignoreCase: true, out var parsed)
                || !Enum.IsDefined(parsed))
            {
                return SkillResult.Error(
                    $"Unknown maxAction '{maxActionText}'. Use Hint, Prepare or Execute.");
            }

            maxAction = parsed;
        }

        var command = new SetProactiveGovernanceCommand(
            TriggerKind: GetParameter<string?>(parameters, TriggerKindParameter),
            GroupId: GetParameter<Guid?>(parameters, GroupIdParameter),
            MaxAction: maxAction,
            Enabled: GetParameter<bool?>(parameters, EnabledParameter),
            ResponsibleOwnerUserId: GetParameter<Guid?>(parameters, ResponsibleOwnerParameter),
            ClearResponsibleOwner: GetParameter<bool?>(parameters, ClearResponsibleOwnerParameter) ?? false,
            DailyActionBudget: GetParameter<int?>(parameters, DailyActionBudgetParameter),
            WindowActionLimit: GetParameter<int?>(parameters, WindowActionLimitParameter),
            WindowMinutes: GetParameter<int?>(parameters, WindowMinutesParameter),
            KillSwitch: GetParameter<bool?>(parameters, KillSwitchParameter));

        var governance = await _mediator.Send(command, cancellationToken);

        var summary = governance.KillSwitchActive
            ? "Proactive governance saved. The global kill switch is ON, so every kind is pinned to Hint."
            : "Proactive governance saved.";

        return SkillResult.SuccessResult(governance, summary);
    }
}
