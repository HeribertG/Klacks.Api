// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The push direction shared by every recipe forcing plan: while active, the chat loop narrows the tool
/// scope to the current step skill, injects captured values into its parameters, and advances by
/// observing the executed calls. Both the hardcoded cut recipe and the data-driven recipe engine
/// implement this so the loop's forcing mechanic is written once.
/// </summary>

using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IRecipeForcingPlan
{
    string Name { get; }

    bool IsActive { get; }

    string? CurrentSkill { get; }

    string? CurrentStepNote { get; }

    IReadOnlyDictionary<string, object> GetParameterInjections(string skillName);

    void Observe(IEnumerable<LLMFunctionCall> executedCalls);
}
