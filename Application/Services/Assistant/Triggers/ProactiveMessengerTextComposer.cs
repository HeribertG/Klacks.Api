// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Renders a proactive trigger event into a readable sentence for a messenger, in the language the
/// installation runs in (decision E56). There is no per-user language anywhere on the server - the
/// browser keeps its choice in local storage and no request header carries it - so the only honest
/// source is the installation-wide DEFAULT_LANGUAGE setting. This mirrors what SlackOwnerBridgeService
/// already does for inbound Slack messages, which face the same "no browser session" situation.
/// Never throws: a failed settings lookup degrades to the fallback language, and an unknown key
/// degrades to the previous behaviour (bare key plus its values) rather than costing the recipient
/// an alert.
/// </summary>
/// <param name="settingsReader">Reads the installation-wide DEFAULT_LANGUAGE setting.</param>
/// <param name="logger">Records a settings lookup that failed and the language it fell back to.</param>

using System.Text;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Settings;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class ProactiveMessengerTextComposer : IProactiveMessengerTextComposer
{
    private const string ParamSeparator = ", ";
    private const string ParamAssignment = ": ";
    private const string LineSeparator = "\n";

    private readonly ISettingsReader _settingsReader;
    private readonly ILogger<ProactiveMessengerTextComposer> _logger;

    public ProactiveMessengerTextComposer(
        ISettingsReader settingsReader,
        ILogger<ProactiveMessengerTextComposer> logger)
    {
        _settingsReader = settingsReader;
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
        var language = await ResolveLanguageAsync();

        if (!MessengerProactiveTexts.TryGetText(key, language, out var template))
        {
            // Not a failure of this event: the key simply is not one of the few the messenger can
            // carry. Keeping the old key-plus-values shape still names the event and its facts.
            _logger.LogDebug(
                "No messenger text for proactive key {Key}; falling back to the raw key for trigger {Kind}",
                key,
                triggerEvent.Kind);
            return AppendRawParams(key, triggerEvent.SummaryParams);
        }

        return Substitute(template, triggerEvent.SummaryParams);
    }

    private async Task<string> ResolveLanguageAsync()
    {
        try
        {
            var setting = await _settingsReader.GetSetting(SettingKeys.DefaultLanguage);
            var configured = setting?.Value;
            if (!string.IsNullOrWhiteSpace(configured)
                && LanguageConfig.SupportedLanguages.Contains(configured, StringComparer.OrdinalIgnoreCase))
            {
                return configured;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Could not read the installation language; the messenger message goes out in {Language}",
                LanguageConfig.DefaultLanguageFallback);
        }

        return LanguageConfig.DefaultLanguageFallback;
    }

    /// <summary>
    /// Replaces every {{name}} placeholder that has a matching parameter. A placeholder without a
    /// value is left standing on purpose: removing it would produce a grammatically complete
    /// sentence that silently claims a fact nobody supplied, whereas a visible {{days}} tells the
    /// reader - and whoever reads the log afterwards - that a value was missing.
    /// </summary>
    private static string Substitute(string template, IReadOnlyDictionary<string, string>? summaryParams)
    {
        if (summaryParams == null || summaryParams.Count == 0)
        {
            return template;
        }

        var builder = new StringBuilder(template);
        foreach (var pair in summaryParams)
        {
            builder.Replace(
                MessengerProactiveTexts.PlaceholderPrefix + pair.Key + MessengerProactiveTexts.PlaceholderSuffix,
                pair.Value);
        }

        return builder.ToString();
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
