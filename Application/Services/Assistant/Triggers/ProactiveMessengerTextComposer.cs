// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Renders a proactive trigger event into a readable sentence for a messenger, in the language the
/// installation runs in (decision E56). There is no per-user language anywhere on the server - the
/// browser keeps its choice in local storage and no request header carries it - so the only honest
/// source is the installation-wide DEFAULT_LANGUAGE setting, read through IInstallationLanguageResolver:
/// a pack language such as ja or zh-CN reaches its own sentence, an unknown or unreadable one degrades to
/// English. The four core languages come from MessengerProactiveTexts, the pack languages from their pack's
/// translations.json; a language whose loaded pack lacks the key falls back to English with a warning, and
/// the catalogue guard keeps that case from shipping.
/// Never throws on bad data: a failed settings lookup degrades to the fallback language, and an unknown key
/// degrades to the previous behaviour (bare key plus its values) rather than costing the recipient
/// an alert. The one exception is a cancelled call, which rethrows so a stopping caller is not held up.
/// </summary>
/// <param name="languageResolver">Resolves the installation-wide DEFAULT_LANGUAGE.</param>
/// <param name="logger">Records a catalogue key a language pack lacks.</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Services.Common;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class ProactiveMessengerTextComposer : IProactiveMessengerTextComposer
{
    private const string ParamSeparator = ", ";
    private const string ParamAssignment = ": ";
    private const string LineSeparator = "\n";

    private readonly IInstallationLanguageResolver _languageResolver;
    private readonly ILogger<ProactiveMessengerTextComposer> _logger;

    public ProactiveMessengerTextComposer(
        IInstallationLanguageResolver languageResolver,
        ILogger<ProactiveMessengerTextComposer> logger)
    {
        _languageResolver = languageResolver;
        _logger = logger;
    }

    public async Task<string> ComposeAsync(IAgentTriggerEvent triggerEvent, CancellationToken cancellationToken = default)
    {
        var summary = triggerEvent.Summary;
        if (!summary.StartsWith(ProactiveMessageMarkers.I18nPrefix, StringComparison.Ordinal))
        {
            return summary;
        }

        var key = summary[ProactiveMessageMarkers.I18nPrefix.Length..];

        if (!MessengerProactiveTexts.Covers(key))
        {
            // Not a failure of this event: the key simply is not one of the few the messenger can
            // carry. Keeping the old key-plus-values shape still names the event and its facts.
            _logger.LogDebug(
                "No messenger text for proactive key {Key}; falling back to the raw key for trigger {Kind}",
                key,
                triggerEvent.Kind);
            return AppendRawParams(key, triggerEvent.SummaryParams);
        }

        var language = await _languageResolver.ResolveAsync(cancellationToken);
        return DoubleBraceTemplate.Render(Resolve(language, key), triggerEvent.SummaryParams);
    }

    private string Resolve(string language, string key)
    {
        if (MessengerProactiveTexts.TryGetText(key, language, out var template))
        {
            return template;
        }

        _logger.LogWarning(
            "The language pack of {Language} has no messenger text {Key}; using the English text", language, key);
        return MessengerProactiveTexts.EnglishOf(key);
    }

    private static string AppendRawParams(string body, IReadOnlyDictionary<string, string>? summaryParams)
    {
        if (summaryParams == null || summaryParams.Count == 0)
        {
            return body;
        }

        var details = string.Join(
            ParamSeparator,
            summaryParams.Select(pair => pair.Key + ParamAssignment + pair.Value));

        return body + LineSeparator + details;
    }
}
