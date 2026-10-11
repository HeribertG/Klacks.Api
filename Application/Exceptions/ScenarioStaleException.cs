// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Globalization;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Exceptions;

/// <summary>
/// An accept was requested for a scenario whose original plan changed after the scenario was created (another
/// scenario accepted in between, edits, seals or a background recalculation) and the caller did not acknowledge that accepting overwrites those
/// changes. Refused as a 409 with its own code so a client can ask for that acknowledgement and retry with
/// overrideStale; it is deliberately separate from the compliance block and its supervisor override.
/// </summary>
/// <param name="staleness">What changed since the scenario was created (count, last change, accepted sibling scenarios)</param>
public sealed class ScenarioStaleException : ConflictException
{
    public const string ErrorCode = "SCENARIO_STALE";

    public const string ChangedEntryCountField = "changedEntryCount";

    public const string LastChangeUtcField = "lastChangeUtc";

    public const string AcceptedScenarioNamesField = "acceptedScenarioNames";

    private const string MessageFormat =
        "The original plan was changed after this scenario was created ({0} changed entries in the scenario's scope, last change {1:O} UTC{2}). Accepting it overwrites those changes; accept with overrideStale to do so anyway.";

    private const string AcceptedScenariosFormat = "; scenarios accepted since: {0}";

    public ScenarioStaleException(ScenarioStaleness staleness)
        : base(BuildMessage(staleness), ErrorCode, BuildDetails(staleness))
    {
        Staleness = staleness;
    }

    public ScenarioStaleness Staleness { get; }

    private static string BuildMessage(ScenarioStaleness staleness)
    {
        var accepted = staleness.AcceptedScenarioNames.Count == 0
            ? string.Empty
            : string.Format(CultureInfo.InvariantCulture, AcceptedScenariosFormat, string.Join(", ", staleness.AcceptedScenarioNames));

        return string.Format(
            CultureInfo.InvariantCulture,
            MessageFormat,
            staleness.ChangedEntryCount,
            staleness.LastChangeUtc,
            accepted);
    }

    private static IReadOnlyDictionary<string, object?> BuildDetails(ScenarioStaleness staleness)
        => new Dictionary<string, object?>
        {
            [ChangedEntryCountField] = staleness.ChangedEntryCount,
            [LastChangeUtcField] = DateTime.SpecifyKind(staleness.LastChangeUtc, DateTimeKind.Utc),
            [AcceptedScenarioNamesField] = staleness.AcceptedScenarioNames,
        };
}
