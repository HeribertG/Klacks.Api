// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Services.Assistant.Skills.Adapters;

public class SkillAdapterFactory : ISkillAdapterFactory
{
    private readonly Dictionary<LLMProviderType, ISkillAdapter> _adapters;

    public SkillAdapterFactory()
    {
        _adapters = new Dictionary<LLMProviderType, ISkillAdapter>
        {
            { LLMProviderType.OpenAI, new OpenAISkillAdapter() },
            { LLMProviderType.Anthropic, new AnthropicSkillAdapter() },
            { LLMProviderType.Google, new GeminiSkillAdapter() },
            { LLMProviderType.Mistral, new MistralSkillAdapter() },
            { LLMProviderType.Azure, new OpenAISkillAdapter() },
            { LLMProviderType.Cohere, new OpenAISkillAdapter() },
            { LLMProviderType.HuggingFace, new OpenAISkillAdapter() }
        };
    }

    public ISkillAdapter GetAdapter(LLMProviderType providerType)
    {
        if (_adapters.TryGetValue(providerType, out var adapter))
        {
            return adapter;
        }

        return _adapters[LLMProviderType.OpenAI];
    }

    public IReadOnlyList<ISkillAdapter> GetAllAdapters()
    {
        return _adapters.Values.Distinct().ToList();
    }
}
