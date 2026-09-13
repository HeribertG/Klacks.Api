// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The effective autonomy level the next-period automation may act at, together with the admin whose
/// stored preference produced it. The admin is carried alongside the level because an automatic accept
/// is not an act without an author: it is released by the standing consent of the most cautious admin,
/// and that is who the audit records as the approving user.
/// </summary>
/// <param name="EffectiveLevel">Minimum over all admins, capped by the global proactive autonomy level.</param>
/// <param name="DecidingAdminUserId">
/// The admin whose preference set <see cref="EffectiveLevel"/>; ties go to the ordinally first admin id.
/// Null when there are no admins, when the global cap - not an admin - decided, or when the admin id is
/// not a Guid. A caller that needs an author must treat null as "no author", never as Guid.Empty.
/// </param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record NextPeriodAutonomyDecision(AutonomyLevel EffectiveLevel, Guid? DecidingAdminUserId);
