using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant.Skills.Adapters;

public interface ISkillAdapter
{
    LLMProviderType ProviderType { get; }

    object ConvertSkillToProviderFormat(ISkill skill);

    SkillInvocation ParseProviderCall(object providerFunctionCall);

    object ConvertResultToProviderFormat(SkillResult result);
}
