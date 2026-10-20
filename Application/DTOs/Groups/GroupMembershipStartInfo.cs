// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Text.Json.Serialization;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Groups;

/// <summary>
/// The facts about the start of new group memberships that a grouping skill reports with its preview and its result,
/// so the model cannot drop them: the start date, how many of the affected persons entered the company earlier and are
/// still employed at the start (the schedule shows their days between entry and group start blue and locked), the earliest
/// such entry and, in EntryDate mode, how many existing members already hold a start that lies after their entry and were
/// left unchanged.
/// </summary>
/// <param name="Mode">Fixed (one date for everybody) or EntryDate (each person starts at their own employment entry).</param>
/// <param name="StartDateGiven">False when the caller supplied no start date and the default (normally today) is the reference.</param>
/// <param name="StartDate">The common start date (yyyy-MM-dd); null in EntryDate mode.</param>
/// <param name="EarliestStartDate">Earliest start of any affected person (yyyy-MM-dd); null without persons.</param>
/// <param name="LatestStartDate">Latest start of any affected person (yyyy-MM-dd); null without persons.</param>
/// <param name="PersonCount">Number of persons whose new membership this describes.</param>
/// <param name="PersonsEnteredBeforeStartCount">Persons whose employment entry lies before their membership start and who are still employed then; their days in between are shown blue and locked.</param>
/// <param name="PersonsWithoutEntryDateCount">Persons without a membership row (no entry date); in EntryDate mode they start on the fallback day.</param>
/// <param name="EarliestEntryDate">Earliest employment entry among the persons entered before the start (yyyy-MM-dd); null when none.</param>
/// <param name="ExistingMembersStartingAfterEntryCount">EntryDate mode only: existing members whose membership starts after their employment entry. This run does not change them.</param>
/// <param name="ExistingMembersEarliestEntryDate">Earliest employment entry among those existing members (yyyy-MM-dd); null when none.</param>
public sealed record GroupMembershipStartInfo(
    [property: JsonConverter(typeof(JsonStringEnumConverter<GroupMembershipStartMode>))]
    GroupMembershipStartMode Mode,
    bool StartDateGiven,
    string? StartDate,
    string? EarliestStartDate,
    string? LatestStartDate,
    int PersonCount,
    int PersonsEnteredBeforeStartCount,
    int PersonsWithoutEntryDateCount,
    string? EarliestEntryDate,
    int ExistingMembersStartingAfterEntryCount = 0,
    string? ExistingMembersEarliestEntryDate = null);
