// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Gives a skill execution context without a UI language the installation language. Paths without a browser
/// session (scheduled tasks, goal plans, proactive actions, inbound messages, MCP and REST skill calls) arrive with
/// a null language; left null, an ambiguous date or time would be read with the historical culture list, whose
/// first entries are Swiss. The installation language is the closest answer to how this installation's users write
/// dates and keeps the reading identical for every channel. A context that already carries a language, or a scope
/// without an installation language resolver, is returned unchanged.
/// </summary>

using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant.Skills;

internal static class InstallationLanguageFallback
{
    /// <param name="context">Execution context as built by the calling path</param>
    /// <param name="serviceProvider">Scope the executor runs in; the resolver is scoped</param>
    /// <param name="cancellationToken">Cancels the settings read</param>
    public static async Task<SkillExecutionContext> ApplyAsync(
        SkillExecutionContext context,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(context.UserLanguage))
        {
            return context;
        }

        if (serviceProvider.GetService(typeof(IInstallationLanguageResolver)) is not IInstallationLanguageResolver resolver)
        {
            return context;
        }

        var installationLanguage = await resolver.ResolveAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(installationLanguage)
            ? context
            : context with { UserLanguage = installationLanguage };
    }
}
