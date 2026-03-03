// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Services.Assistant.Skills.Adapters;

public interface ISkillAdapterFactory
{
    ISkillAdapter GetAdapter(LLMProviderType providerType);
    IReadOnlyList<ISkillAdapter> GetAllAdapters();
}
