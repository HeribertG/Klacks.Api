// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Hard read-only filter for the research sub-loop toolset: keeps only skills the
/// <see cref="ISkillRiskClassifier"/> classifies as <see cref="SkillRiskClass.ReadOnly"/>, drops the
/// skills in <see cref="DraftPersistingReadOnlySkills"/> (ReadOnly for the autonomy gate, but they persist
/// the owner's drafts or a plan with a confirmation token - a research loop must change nothing) and drops a
/// caller-supplied skill name (the research skill itself, to break recursion). Every mutating class
/// (Reversible, ScenarioGated, Sensitive, Irreversible) is excluded so a state-changing skill can never
/// reach the cheap sub-loop model. When the research runs on behalf of an external agent (MCP), the toolset
/// is further capped to what that caller could call over MCP directly: the MCP exposure policy (no UI,
/// no Sensitive, no list_personal_access_tokens) and the read-mode policy of its access mode.
/// </summary>
/// <param name="classifier">Risk classifier that decides read-only vs. mutating per skill descriptor.</param>
/// <param name="exposurePolicy">Which skills MCP exposes to external agents at all.</param>
/// <param name="readModeToolPolicy">Which exposed skills an MCP caller may use under its access mode.</param>

using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant;

public class ReadOnlyToolsetFilter : IReadOnlyToolsetFilter
{
    private readonly ISkillRiskClassifier _classifier;
    private readonly IMcpSkillExposurePolicy _exposurePolicy;
    private readonly IMcpReadModeToolPolicy _readModeToolPolicy;

    public ReadOnlyToolsetFilter(
        ISkillRiskClassifier classifier,
        IMcpSkillExposurePolicy exposurePolicy,
        IMcpReadModeToolPolicy readModeToolPolicy)
    {
        _classifier = classifier;
        _exposurePolicy = exposurePolicy;
        _readModeToolPolicy = readModeToolPolicy;
    }

    public IReadOnlyList<SkillDescriptor> Filter(
        IReadOnlyList<SkillDescriptor> candidates,
        string? excludeSkillName,
        PersonalAccessTokenAccessMode? externalAgentAccessMode)
    {
        return candidates
            .Where(descriptor => !IsExcludedByName(descriptor, excludeSkillName))
            .Where(descriptor => !DraftPersistingReadOnlySkills.Contains(descriptor.Name))
            .Where(descriptor => _classifier.Classify(descriptor) == SkillRiskClass.ReadOnly)
            .Where(descriptor => IsReachableForExternalAgent(descriptor, externalAgentAccessMode))
            .ToList();
    }

    private bool IsReachableForExternalAgent(
        SkillDescriptor descriptor,
        PersonalAccessTokenAccessMode? externalAgentAccessMode)
    {
        if (externalAgentAccessMode is not { } accessMode)
        {
            return true;
        }

        return _exposurePolicy.IsExposed(descriptor)
            && _readModeToolPolicy.IsAllowed(descriptor, accessMode);
    }

    private static bool IsExcludedByName(SkillDescriptor descriptor, string? excludeSkillName)
    {
        return !string.IsNullOrEmpty(excludeSkillName)
            && string.Equals(descriptor.Name, excludeSkillName, StringComparison.OrdinalIgnoreCase);
    }
}
