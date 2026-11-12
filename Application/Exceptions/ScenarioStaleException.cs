// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Globalization;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Exceptions;

/// <summary>
/// An accept was requested for a scenario whose original plan changed after the scenario was created (another
/// scenario accepted in between, edits, seals or a background recalculation) and the caller did not acknowledge that accepting overwrites those
/// changes. Refused as a 409 with its own code so a client can ask for that acknowledgement and retry with
/// overrideStale; it is deliberately separate from the compliance block and its supervisor override.
/// A scenario whose source chain starts at the cover of an absence built on the real plan (the cover itself or a scenario
/// built on it) is refused as not overridable (overridable false): no acknowledgement passes it, the scenario has to be
/// discarded and the absence covered again, prefilled from the scenario's own absences that are not real yet
/// (coverClientId, coverAbsenceId, coverFromDate, coverUntilDate; null when not unique). The overridable field is always
/// present.
/// </summary>
/// <param name="staleness">What changed since the scenario was created (count, last change, accepted sibling scenarios)</param>
public sealed class ScenarioStaleException : ConflictException
{
    public const string ErrorCode = "SCENARIO_STALE";

    public const string ChangedEntryCountField = "changedEntryCount";

    public const string LastChangeUtcField = "lastChangeUtc";

    public const string AcceptedScenarioNamesField = "acceptedScenarioNames";

    public const string OverridableField = "overridable";

    public const string CoverClientIdField = "coverClientId";

    public const string CoverAbsenceIdField = "coverAbsenceId";

    public const string CoverFromDateField = "coverFromDate";

    public const string CoverUntilDateField = "coverUntilDate";

    private const string MessageFormat =
        "The original plan was changed after this scenario was created ({0} changed entries in the scenario's scope, last change {1:O} UTC{2}). Accepting it overwrites those changes; accept with overrideStale to do so anyway.";

    private const string NotOverridableMessageFormat =
        "The original plan was changed after the absence cover this scenario starts from was created ({0} changed entries in the scenario's scope, last change {1:O} UTC{2}). A cover built on the original plan, and every scenario built on it, cannot overwrite those changes; discard it and cover the absence again.";

    private const string AcceptedScenariosFormat = "; scenarios accepted since: {0}";

    public ScenarioStaleException(ScenarioStaleness staleness)
        : base(BuildMessage(MessageFormat, staleness), ErrorCode, BuildDetails(staleness, coverRedo: null))
    {
        Staleness = staleness;
        Overridable = true;
    }

    private ScenarioStaleException(ScenarioStaleness staleness, CoverRedoPreset coverRedo)
        : base(BuildMessage(NotOverridableMessageFormat, staleness), ErrorCode, BuildDetails(staleness, coverRedo))
    {
        Staleness = staleness;
        Overridable = false;
        CoverRedo = coverRedo;
    }

    public ScenarioStaleness Staleness { get; }

    /// <summary>False when the source chain starts at the cover of an absence built on the real plan: no stale override applies.</summary>
    public bool Overridable { get; }

    /// <summary>Prefill for covering the absence again; null when the warning is overridable.</summary>
    public CoverRedoPreset? CoverRedo { get; }

    /// <summary>The refusal of a stale scenario whose source chain starts at a cover built on the real plan, which no acknowledgement lifts.</summary>
    public static ScenarioStaleException NotOverridable(ScenarioStaleness staleness, CoverRedoPreset? coverRedo)
        => new(staleness, coverRedo ?? CoverRedoPreset.Empty);

    private static string BuildMessage(string format, ScenarioStaleness staleness)
    {
        var accepted = staleness.AcceptedScenarioNames.Count == 0
            ? string.Empty
            : string.Format(CultureInfo.InvariantCulture, AcceptedScenariosFormat, string.Join(", ", staleness.AcceptedScenarioNames));

        return string.Format(
            CultureInfo.InvariantCulture,
            format,
            staleness.ChangedEntryCount,
            staleness.LastChangeUtc,
            accepted);
    }

    /// <summary>
    /// The 409 body; the prefill fields are present exactly when the warning is not overridable (coverRedo set).
    /// </summary>
    private static IReadOnlyDictionary<string, object?> BuildDetails(ScenarioStaleness staleness, CoverRedoPreset? coverRedo)
    {
        var details = new Dictionary<string, object?>
        {
            [ChangedEntryCountField] = staleness.ChangedEntryCount,
            [LastChangeUtcField] = DateTime.SpecifyKind(staleness.LastChangeUtc, DateTimeKind.Utc),
            [AcceptedScenarioNamesField] = staleness.AcceptedScenarioNames,
            [OverridableField] = coverRedo is null,
        };

        if (coverRedo is not null)
        {
            details[CoverClientIdField] = coverRedo.ClientId;
            details[CoverAbsenceIdField] = coverRedo.AbsenceId;
            details[CoverFromDateField] = coverRedo.FromDate;
            details[CoverUntilDateField] = coverRedo.UntilDate;
        }

        return details;
    }
}
