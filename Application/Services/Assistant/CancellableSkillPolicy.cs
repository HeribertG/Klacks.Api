// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The stop-token rule of a streamed turn: a skill is cut short only when its risk class is ReadOnly, it is
/// not one of the skills that are ReadOnly by allow-list exception, and it is no UI action. Neither the name
/// prefix alone nor the risk class alone is safe: check_erp_drop_point_folder_health has a read-only prefix
/// and creates the folder when it is missing, and create_plan is classified ReadOnly although it stores a
/// plan and a confirmation token, so an abort between the two writes would leave an orphaned draft. An
/// unknown skill never receives the token.
/// </summary>
/// <param name="skillRegistry">Resolves the skill's descriptor by name</param>
/// <param name="riskClassifier">Classifies the descriptor into its risk class</param>

using Klacks.Api.Application.Skills.Meta;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant;

public class CancellableSkillPolicy : ICancellableSkillPolicy
{
    private readonly ISkillRegistry _skillRegistry;
    private readonly ISkillRiskClassifier _riskClassifier;

    public CancellableSkillPolicy(ISkillRegistry skillRegistry, ISkillRiskClassifier riskClassifier)
    {
        _skillRegistry = skillRegistry;
        _riskClassifier = riskClassifier;
    }

    public bool ReceivesStopToken(string skillName)
    {
        var descriptor = _skillRegistry.GetSkillByName(skillName);
        if (descriptor == null
            || string.Equals(descriptor.ExecutionType, LlmExecutionTypes.UiAction, StringComparison.OrdinalIgnoreCase)
            || SkillRiskClassifier.ReadOnlyExtras.Contains(descriptor.Name))
        {
            return false;
        }

        return _riskClassifier.Classify(descriptor) == SkillRiskClass.ReadOnly;
    }
}
