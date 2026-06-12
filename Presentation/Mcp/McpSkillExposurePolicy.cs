// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Decides which skills are exposed as MCP tools to external agents: only backend-executable
/// skills (no UI actions, no UI navigation) that are not classified as sensitive.
/// </summary>
/// <param name="descriptor">Skill whose execution type, category and risk class are evaluated</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Presentation.Mcp;

public class McpSkillExposurePolicy : IMcpSkillExposurePolicy
{
    private readonly ISkillRiskClassifier _riskClassifier;

    public McpSkillExposurePolicy(ISkillRiskClassifier riskClassifier)
    {
        _riskClassifier = riskClassifier;
    }

    public bool IsExposed(SkillDescriptor descriptor)
    {
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
