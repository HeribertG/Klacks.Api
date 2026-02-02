using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Services.Skills.Adapters;

public interface ISkillAdapterFactory
{
    ISkillAdapter GetAdapter(LLMProviderType providerType);
    IReadOnlyList<ISkillAdapter> GetAllAdapters();
}
