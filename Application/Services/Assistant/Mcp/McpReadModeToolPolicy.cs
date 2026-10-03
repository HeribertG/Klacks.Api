// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Decides which already-exposed MCP tools a caller may use under its access mode. Write allows every
/// exposed tool. Read allows only skills classified SkillRiskClass.ReadOnly, never confirm_pending_action
/// (it redeems held write actions) and never the skills in DraftPersistingReadOnlySkills, which classify
/// ReadOnly for the autonomy gate but still persist rows (create_plan's draft plan, the owner's company-rule and
/// planning-profile draft), so a read-only agent could overwrite or discard a draft the owner is editing in the chat.
/// Used by McpToolCatalog for listing, again by McpSkillCallHandler for every call (defense in depth: a client may
/// call a tool it never listed) and by ReadOnlyToolsetFilter for the research sub-loop run_analysis starts.
/// </summary>
/// <param name="riskClassifier">Classifies skills into risk classes; only ReadOnly passes under Read</param>

using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Mcp;

public class McpReadModeToolPolicy : IMcpReadModeToolPolicy
{
    private readonly ISkillRiskClassifier _riskClassifier;

    public McpReadModeToolPolicy(ISkillRiskClassifier riskClassifier)
    {
        _riskClassifier = riskClassifier;
    }

    public bool IsAllowed(SkillDescriptor descriptor, PersonalAccessTokenAccessMode accessMode)
    {
        if (accessMode == PersonalAccessTokenAccessMode.Write)
        {
            return true;
        }

        if (string.Equals(descriptor.Name, AutonomyDefaults.ConfirmPendingActionSkillName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (DraftPersistingReadOnlySkills.Contains(descriptor.Name))
        {
            return false;
        }

        return _riskClassifier.Classify(descriptor) == SkillRiskClass.ReadOnly;
    }
}
