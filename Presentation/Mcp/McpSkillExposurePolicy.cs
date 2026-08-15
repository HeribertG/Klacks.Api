// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Decides which skills are exposed as MCP tools to external agents: only backend-executable
/// skills (no UI actions, no UI navigation) that are neither classified as sensitive nor named on
/// this policy's own exclusion list. The exclusion list exists because SkillRiskClass.Sensitive is
/// a bundled lever — it hides a skill from MCP but also forces a chat confirmation, blocks plan and
/// recurring-task steps. A read-only skill that must stay invisible to external agents without
/// paying that price belongs here instead of in SkillRiskClassifier.SensitiveSkills.
/// </summary>
/// <param name="descriptor">Skill whose name, execution type, category and risk class are evaluated</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Presentation.Mcp;

public class McpSkillExposurePolicy : IMcpSkillExposurePolicy
{
    // Skills withheld from external agents for a reason that is NOT their risk class. The /mcp
    // endpoint accepts personal-access-token authentication, so a stolen token must not be able to
    // enumerate the remaining tokens of its owner — the "a PAT cannot manage PATs" rule that
    // PersonalAccessTokensController enforces by pinning itself to JWT bearer. Listing is a pure
    // read of the caller's own metadata, so it is gated here instead of via SensitiveSkills.
    private static readonly HashSet<string> ExcludedSkills = new(StringComparer.OrdinalIgnoreCase)
    {
        "list_personal_access_tokens"
    };

    private readonly ISkillRiskClassifier _riskClassifier;

    public McpSkillExposurePolicy(ISkillRiskClassifier riskClassifier)
    {
        _riskClassifier = riskClassifier;
    }

    public bool IsExposed(SkillDescriptor descriptor)
    {
        if (ExcludedSkills.Contains(descriptor.Name))
        {
            return false;
        }

        if (!string.Equals(descriptor.ExecutionType, LlmExecutionTypes.Skill, StringComparison.Ordinal))
        {
            return false;
        }

        if (descriptor.Category == SkillCategory.UI)
        {
            return false;
        }

        return _riskClassifier.Classify(descriptor) != SkillRiskClass.Sensitive;
    }
}
