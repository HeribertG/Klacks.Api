// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Renders the escalation handoff sentences in the installation language (DEFAULT_LANGUAGE, possibly a
/// language-pack language such as ja or zh-CN). The catalogue supplies the template, DoubleBraceTemplate
/// fills it in one pass, so a value such as an employee name is never expanded twice. A language whose
/// loaded pack lacks a key falls back to English with a warning in the log and the note is still written;
/// the catalogue guard keeps that case from shipping. That warning is NOT a promise that English can never
/// appear unannounced: a language counts as loaded only when its assistant-texts.json was read at startup,
/// so a pack installed after the process started is an unknown language here and resolves to English
/// without a warning. Scoped, like the language resolver it uses.
/// </summary>
/// <param name="languageResolver">Resolves the installation language</param>
/// <param name="logger">Logs a missing catalogue key</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Services.Common;

namespace Klacks.Api.Infrastructure.Services.Assistant.Escalation;

public sealed class EscalationHandoffTextService : IEscalationHandoffTextService
{
    private readonly IInstallationLanguageResolver _languageResolver;
    private readonly ILogger<EscalationHandoffTextService> _logger;

    public EscalationHandoffTextService(
        IInstallationLanguageResolver languageResolver, ILogger<EscalationHandoffTextService> logger)
    {
        _languageResolver = languageResolver;
        _logger = logger;
    }

    public async Task<string> RenderAsync(
        string key, IReadOnlyDictionary<string, string> parameters, CancellationToken cancellationToken = default)
    {
        var language = await _languageResolver.ResolveAsync(cancellationToken);
        return DoubleBraceTemplate.Render(Resolve(language, key), parameters);
    }

    private string Resolve(string language, string key)
    {
        if (EscalationHandoffTexts.TryGetText(key, language, out var text))
        {
            return text;
        }

        _logger.LogWarning(
            "The language pack of {Language} has no escalation handoff text {Key}; using the English text", language, key);
        return EscalationHandoffTexts.EnglishOf(key);
    }
}
