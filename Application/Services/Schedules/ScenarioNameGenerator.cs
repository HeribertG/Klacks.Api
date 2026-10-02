// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Builds the name of a scenario the server creates on its own: a localized prefix (ScenarioNameTexts), the
/// period as ISO-8601 calendar dates and, when the group already has a scenario of that name, a counter
/// suffix " (2)", " (3)", ... The dates are culture-neutral on purpose: the name is written once and read in
/// every language afterwards, ISO dates are unambiguous for every reader (no day/month swap between en-US and
/// the rest), need no calendar mapping (ar-SA, th-TH are not Gregorian) and are pure calendar values without a
/// time or time zone. The prefix language is the planner's language when one is passed and a catalogue claims
/// it, otherwise the installation language; English only as the last floor when even that has no text.
/// </summary>
/// <param name="scenarioRepository">Reads the existing scenario names of the group for the uniqueness check</param>
/// <param name="installationLanguageResolver">Resolves the installation language when no usable language is passed</param>
/// <param name="logger">Warns when a loaded language pack lacks a scenario name text</param>

using System.Globalization;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Settings;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services.Schedules;

public sealed class ScenarioNameGenerator : IScenarioNameGenerator
{
    private const string DateFormat = "yyyy-MM-dd";
    private const string SingleDayPattern = "{0} {1}";
    private const string PeriodPattern = "{0} {1} – {2}";
    private const string CounterPattern = "{0} ({1})";
    private const int FirstCounter = 2;

    private readonly IAnalyseScenarioRepository _scenarioRepository;
    private readonly IInstallationLanguageResolver _installationLanguageResolver;
    private readonly ILogger<ScenarioNameGenerator> _logger;

    public ScenarioNameGenerator(
        IAnalyseScenarioRepository scenarioRepository,
        IInstallationLanguageResolver installationLanguageResolver,
        ILogger<ScenarioNameGenerator> logger)
    {
        _scenarioRepository = scenarioRepository;
        _installationLanguageResolver = installationLanguageResolver;
        _logger = logger;
    }

    public async Task<string> GenerateAsync(
        ScenarioNameKind kind,
        DateOnly from,
        DateOnly until,
        Guid? groupId,
        string? language,
        CancellationToken cancellationToken)
    {
        var prefix = await ResolvePrefixAsync(kind, language, cancellationToken);
        var baseName = BuildBaseName(prefix, from, until);

        var existing = await _scenarioRepository.GetByGroupAsync(groupId, cancellationToken);
        var existingNames = existing.Select(s => s.Name).ToHashSet(StringComparer.Ordinal);

        if (!existingNames.Contains(baseName))
        {
            return baseName;
        }

        var counter = FirstCounter;
        while (existingNames.Contains(WithCounter(baseName, counter)))
        {
            counter++;
        }

        return WithCounter(baseName, counter);
    }

    private async Task<string> ResolvePrefixAsync(ScenarioNameKind kind, string? language, CancellationToken cancellationToken)
    {
        var key = ScenarioNameTexts.KeyOf(kind);
        if (ScenarioNameTexts.TryGetOwnText(key, language, out var own))
        {
            return own;
        }

        var installationLanguage = await _installationLanguageResolver.ResolveAsync(cancellationToken);
        if (ScenarioNameTexts.TryGetText(key, installationLanguage, out var installed))
        {
            return installed;
        }

        _logger.LogWarning(
            "The language pack {Language} has no scenario name text for {Key}; the name goes out in English",
            installationLanguage, key);
        return ScenarioNameTexts.EnglishOf(key);
    }

    private static string BuildBaseName(string prefix, DateOnly from, DateOnly until)
    {
        var fromText = from.ToString(DateFormat, CultureInfo.InvariantCulture);
        if (from == until)
        {
            return string.Format(CultureInfo.InvariantCulture, SingleDayPattern, prefix, fromText);
        }

        var untilText = until.ToString(DateFormat, CultureInfo.InvariantCulture);
        return string.Format(CultureInfo.InvariantCulture, PeriodPattern, prefix, fromText, untilText);
    }

    private static string WithCounter(string baseName, int counter) =>
        string.Format(CultureInfo.InvariantCulture, CounterPattern, baseName, counter);
}
