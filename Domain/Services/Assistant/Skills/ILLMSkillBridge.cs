// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Services.Assistant.Skills;

public interface ILLMSkillBridge
{
    IReadOnlyList<LLMFunction> GetSkillsAsLLMFunctions(IReadOnlyList<string> userPermissions);

    Task<SkillBridgeResult> ExecuteSkillFromLLMCallAsync(
        LLMFunctionCall functionCall,
        SkillExecutionContext context,
        CancellationToken cancellationToken = default);

    IReadOnlyList<object> GetSkillsForProvider(
        LLMProviderType providerType,
        IReadOnlyList<string> userPermissions);
}
