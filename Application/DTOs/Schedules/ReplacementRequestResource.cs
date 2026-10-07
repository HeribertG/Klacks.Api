// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// One entry of the replacement request book as the API returns it: the raw facts of a candidate proposed or
/// asked for one slot of an absent employee, plus the short-notice verdict computed when reading.
/// </summary>
/// <param name="Id">Row id, used by PUT Recovery/Requests/{id}/Outcome</param>
/// <param name="AbsentClientId">Employee whose slot needs a replacement</param>
/// <param name="CandidateClientId">Employee proposed or asked to step in</param>
/// <param name="ShiftId">Source shift of the slot</param>
/// <param name="Date">Date of the slot</param>
/// <param name="StartTime">Slot start</param>
/// <param name="EndTime">Slot end</param>
/// <param name="GroupId">Group context only (never billing)</param>
/// <param name="AbsenceId">Absence type that caused the gap; null for a manual replacement</param>
/// <param name="Source">Path that created the row</param>
/// <param name="Outcome">Proposed, or the recorded answer of the candidate</param>
/// <param name="ReportedAtUtc">Instant the absence was reported</param>
/// <param name="ShiftStartUtc">Instant the slot starts</param>
/// <param name="OutcomeAtUtc">Instant the answer was recorded</param>
/// <param name="OutcomeByUserId">User who recorded the answer</param>
/// <param name="AnalyseToken">Scenario the proposal lives in; null on the real plan</param>
/// <param name="WorkChangeId">Informative id of the replacement WorkChange</param>
/// <param name="AppliedAtUtc">Instant the replacement reached the real plan</param>
/// <param name="IsShortNotice">True when the report came less than REPLACEMENT_SHORT_NOTICE_HOURS before the slot</param>

using System.Text.Json.Serialization;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Schedules;

public sealed record ReplacementRequestResource(
    Guid Id,
    Guid AbsentClientId,
    Guid CandidateClientId,
    Guid ShiftId,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    Guid? GroupId,
    Guid? AbsenceId,
    [property: JsonConverter(typeof(JsonStringEnumConverter<ReplacementRequestSource>))] ReplacementRequestSource Source,
    [property: JsonConverter(typeof(JsonStringEnumConverter<ReplacementRequestOutcome>))] ReplacementRequestOutcome Outcome,
    DateTime ReportedAtUtc,
    DateTime ShiftStartUtc,
    DateTime? OutcomeAtUtc,
    Guid? OutcomeByUserId,
    Guid? AnalyseToken,
    Guid? WorkChangeId,
    DateTime? AppliedAtUtc,
    bool IsShortNotice);
