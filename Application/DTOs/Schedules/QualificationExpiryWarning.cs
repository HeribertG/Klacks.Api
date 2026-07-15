// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// A held, currently valid, mandatory qualification that will expire within the configured
/// QUALIFICATION_EXPIRY_WARNING_DAYS window — a proactive heads-up, never a blocker.
/// </summary>
/// <param name="QualificationId">The qualification approaching its validity end</param>
/// <param name="ValidUntil">The date the qualification stops being valid</param>
public sealed record QualificationExpiryWarning(Guid QualificationId, DateOnly ValidUntil);
