using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant.Skills.Adapters;

public interface ISkillAdapter
{
    LLMProviderType ProviderType { get; }

    object ConvertSkillToProviderFormat(SkillDescriptor descriptor);

    SkillInvocation ParseProviderCall(object providerFunctionCall);

    object ConvertResultToProviderFormat(SkillResult result);
}
