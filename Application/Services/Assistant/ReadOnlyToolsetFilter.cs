// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Hard read-only filter for the research sub-loop toolset: keeps only skills the
/// <see cref="ISkillRiskClassifier"/> classifies as <see cref="SkillRiskClass.ReadOnly"/> and drops a
/// caller-supplied skill name (the research skill itself, to break recursion). Every mutating class
/// (Reversible, ScenarioGated, Sensitive, Irreversible) is excluded so a state-changing skill can never
/// reach the cheap sub-loop model.
/// </summary>
/// <param name="classifier">Risk classifier that decides read-only vs. mutating per skill descriptor.</param>

using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant;

public class ReadOnlyToolsetFilter : IReadOnlyToolsetFilter
{
    private readonly ISkillRiskClassifier _classifier;

    public ReadOnlyToolsetFilter(ISkillRiskClassifier classifier)
    {
        _classifier = classifier;
    }

    public IReadOnlyList<SkillDescriptor> Filter(
        IReadOnlyList<SkillDescriptor> candidates,
        string? excludeSkillName = null)
    {
        return candidates
            .Where(descriptor => !IsExcludedByName(descriptor, excludeSkillName))
            .Where(descriptor => _classifier.Classify(descriptor) == SkillRiskClass.ReadOnly)
            .ToList();
    }

    private static bool IsExcludedByName(SkillDescriptor descriptor, string? excludeSkillName)
    {
        return !string.IsNullOrEmpty(excludeSkillName)
            && string.Equals(descriptor.Name, excludeSkillName, StringComparison.OrdinalIgnoreCase);
    }
}
