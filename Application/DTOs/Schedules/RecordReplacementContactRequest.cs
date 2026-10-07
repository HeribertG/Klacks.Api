// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Body of POST Recovery/Requests: records the planner's contact attempt with an alternative candidate for one
/// slot (upsert on scenario token, candidate, shift and date). It never changes the proposal itself; swapping
/// the replacement is a separate step.
/// </summary>
/// <param name="AbsentClientId">Employee whose slot needs a replacement</param>
/// <param name="CandidateClientId">Alternative candidate that was contacted</param>
/// <param name="ShiftId">Source shift of the slot</param>
/// <param name="Date">Date of the slot</param>
/// <param name="StartTime">Slot start</param>
/// <param name="EndTime">Slot end</param>
/// <param name="GroupId">Group context only</param>
/// <param name="AbsenceId">Absence type that caused the gap</param>
/// <param name="AnalyseToken">Scenario the slot is reviewed in; null on the real plan</param>
/// <param name="Outcome">Requested, Accepted, Declined or NotReached</param>

using System.Text.Json.Serialization;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Schedules;

public sealed record RecordReplacementContactRequest(
    Guid AbsentClientId,
    Guid CandidateClientId,
    Guid ShiftId,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    Guid? GroupId,
    Guid? AbsenceId,
    Guid? AnalyseToken,
    [property: JsonConverter(typeof(JsonStringEnumConverter<ReplacementRequestOutcome>))] ReplacementRequestOutcome Outcome);
