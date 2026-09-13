// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// What the next-period automation may do right now, with all four brakes already folded together:
/// the global kill switch, the global autonomy level, the governance row's Enabled flag and its
/// MaxAction, plus the minimum autonomy level over all admins. Callers read the two booleans and never
/// re-derive them from EffectiveLevel - governance can only ever lower them, never raise them.
/// The admin is carried alongside because an automatic accept is not an act without an author: it is
/// released by the standing consent of the most cautious admin, and that is who the audit records.
/// </summary>
/// <param name="EffectiveLevel">Minimum over all admins, capped by the global proactive autonomy level.</param>
/// <param name="DecidingAdminUserId">
/// The admin whose preference set <see cref="EffectiveLevel"/>; ties go to the ordinally first admin id.
/// Null when there are no admins, when the global cap - not an admin - decided, or when the admin id is
/// not a Guid. A caller that needs an author must treat null as "no author", never as Guid.Empty.
/// </param>
/// <param name="CanStartAutofill">
/// Starting the wizard chain is the Prepare class: no kill switch, the kind enabled, an effective
/// MaxAction of at least Prepare, and an effective level of at least Autonomous.
/// </param>
/// <param name="CanCommit">
/// Accepting the produced scenario into the real schedule is the Execute class: no kill switch, the
/// kind enabled, an effective MaxAction of at least Execute, and an effective level of exactly
/// FullyAutonomous. Never true when <see cref="CanStartAutofill"/> is false.
/// </param>
/// <param name="BlockedBy">
/// The strongest brake keeping <see cref="CanCommit"/> false, so the watcher can name the cause in its
/// blocked event. None exactly when CanCommit is true.
/// </param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record NextPeriodAutonomyDecision(
    AutonomyLevel EffectiveLevel,
    Guid? DecidingAdminUserId,
    bool CanStartAutofill,
    bool CanCommit,
    NextPeriodAutonomyBlockedBy BlockedBy);
