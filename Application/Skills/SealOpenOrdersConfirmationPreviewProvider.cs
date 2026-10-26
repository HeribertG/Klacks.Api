// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Confirmation preview of seal_open_orders: when the autonomy gate holds the call, this provider resolves it with the
/// very planner the confirmed call uses (ISealOpenOrdersPlanner, a dry-run that writes nothing) and describes the run -
/// how many matched orders are sealable and blocked, what blocks them, what the group assignment would change. It refuses,
/// so the gate issues no token, when the planner refuses the call. A held apply=true is bound to every matched order and
/// its classification (SealOpenOrdersBinding): the confirmed replay seals exactly these orders or nothing. A held
/// apply=false says that it seals nothing; its replay only repeats the preview. It only reads.
/// </summary>
/// <param name="planner">Resolves the call's parameters into the dry-run the confirmed call compares against</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Orders;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills;

namespace Klacks.Api.Application.Skills;

public class SealOpenOrdersConfirmationPreviewProvider : ISkillConfirmationPreviewProvider
{
    private const string ApplyHeadline = "Seal the open orders now. ";

    private const string ApplyNote =
        "Confirming seals the sealable orders counted here, each into an immutable sealed order with its plannable shift; " +
        "sealing can never be undone. Exactly these orders and their classification are bound to the confirmation: if an " +
        "order appears, disappears or changes between sealable and blocked before the user confirms, nothing is sealed and " +
        "a fresh preview is shown.";

    private const string PreviewOnlyHeadline = "Preview only, this call seals nothing. ";

    private readonly ISealOpenOrdersPlanner _planner;

    public SealOpenOrdersConfirmationPreviewProvider(ISealOpenOrdersPlanner planner)
    {
        _planner = planner;
    }

    public bool Supports(string skillName) =>
        string.Equals(skillName, SealOpenOrdersSkill.SkillName, StringComparison.OrdinalIgnoreCase);

    public async Task<SkillConfirmationPreview> BuildAsync(
        string skillName,
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var plan = await _planner.PlanAsync(context, parameters, cancellationToken);
        if (plan.IsRefused)
        {
            return SkillConfirmationPreview.Refuse(plan.Refusal!);
        }

        var facts = SealOpenOrdersPreviewText.Describe(plan.Preview!, plan.Command!.Scope, plan.GroupNote);
        var apply = SkillParameterReader.Read<bool?>(parameters, PreviewApplySkillCalls.ApplyParameter) == true;
        var text = apply ? ApplyHeadline + facts + ApplyNote : PreviewOnlyHeadline + facts;
        return SkillConfirmationPreview.Show(text, SealOpenOrdersBinding.Of(plan.Preview!.Selection));
    }
}
