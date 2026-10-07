// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// One entry of the replacement request book ("Einsprung-Datensatz"): a candidate asked, or proposed, to
/// take over one slot (shift x date) of an absent employee. One row per candidate and slot. It stores raw
/// facts only - when the absence was reported, when the shift starts, who answered how and when - and no
/// free-text reason; "short notice" is judged when reading (REPLACEMENT_SHORT_NOTICE_HOURS). GroupId is
/// context only and never part of billing. WorkChangeId is informative: scenario clones get new ids, so the
/// natural key is (AnalyseToken, CandidateClientId, ShiftId, Date) with ShiftId always the source shift.
/// </summary>
/// <param name="AbsentClientId">Employee whose slot needs a replacement</param>
/// <param name="CandidateClientId">Employee proposed or asked to step in</param>
/// <param name="ShiftId">Source (never scenario clone) shift of the slot</param>
/// <param name="AbsenceId">Absence type that caused the gap (the "why"); null for a manual replacement</param>
/// <param name="Source">Which path created the row</param>
/// <param name="Outcome">Proposed by the engine, or the recorded answer of the candidate</param>
/// <param name="ReportedAtUtc">Instant the absence was reported (inbound receive time for the messenger)</param>
/// <param name="ShiftStartUtc">Slot start converted from the company time zone</param>
/// <param name="AnalyseToken">Scenario the proposal lives in; null for a replacement on the real plan</param>
/// <param name="AppliedAtUtc">Instant the replacement reached the real plan (scenario accept or a direct manual replacement)</param>

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Schedules;

public class ReplacementRequest : BaseEntity
{
    public Guid AbsentClientId { get; set; }

    public Guid CandidateClientId { get; set; }

    public Guid ShiftId { get; set; }

    public DateOnly Date { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public Guid? GroupId { get; set; }

    public Guid? AbsenceId { get; set; }

    public ReplacementRequestSource Source { get; set; }

    public ReplacementRequestOutcome Outcome { get; set; }

    public DateTime ReportedAtUtc { get; set; }

    public DateTime ShiftStartUtc { get; set; }

    public DateTime? OutcomeAtUtc { get; set; }

    public Guid? OutcomeByUserId { get; set; }

    public Guid? AnalyseToken { get; set; }

    public Guid? WorkChangeId { get; set; }

    public DateTime? AppliedAtUtc { get; set; }
}
