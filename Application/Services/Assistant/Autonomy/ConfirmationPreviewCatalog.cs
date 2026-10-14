// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Answers IConfirmationPreviewCatalog from the registered preview providers: a skill has a preview when any provider
/// supports it. Scoped, like the providers it asks.
/// </summary>
/// <param name="providers">Every registered confirmation preview provider</param>

using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Autonomy;

public class ConfirmationPreviewCatalog : IConfirmationPreviewCatalog
{
    private readonly IReadOnlyList<ISkillConfirmationPreviewProvider> _providers;

    public ConfirmationPreviewCatalog(IEnumerable<ISkillConfirmationPreviewProvider> providers)
    {
        _providers = providers.ToList();
    }

    public bool HasPreview(string skillName) => _providers.Any(provider => provider.Supports(skillName));
}
