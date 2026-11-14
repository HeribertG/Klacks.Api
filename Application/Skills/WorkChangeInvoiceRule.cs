// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The chat skills may keep a WorkChange billable but never switch it off: only a planner, supervisor or admin may do that in the schedule dialog.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills;

namespace Klacks.Api.Application.Skills;

internal static class WorkChangeInvoiceRule
{
    public const string ToInvoiceParameter = "toInvoice";

    public const string DisableRefusedMessage =
        "toInvoice=false is refused: a WorkChange stays billable when it is created or changed through the assistant. " +
        "Only a planner, supervisor or admin may switch it off, in the schedule dialog of the correction, travel, briefing or replacement.";

    /// <summary>
    /// Returns a refusal when a skill call tries to disable the invoice rule, otherwise null.
    /// </summary>
    /// <param name="parameters">Raw skill arguments; a boolean or a text such as "false" is read like any other boolean parameter</param>
    public static SkillResult? RefuseDisabling(Dictionary<string, object> parameters)
    {
        var requested = SkillParameterReader.Read<bool?>(parameters, ToInvoiceParameter);
        return requested == false ? SkillResult.Error(DisableRefusedMessage) : null;
    }
}
