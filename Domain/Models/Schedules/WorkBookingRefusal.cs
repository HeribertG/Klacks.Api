// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Schedules;

/// <summary>
/// The non-throwing result of a booking check: why a work write would be refused. The throwing check raises
/// exactly this refusal, so a diagnosis and the real write share one decision.
/// </summary>
/// <param name="Code">Machine code, one of the WorkWriteConflictCodes</param>
/// <param name="Message">English diagnostic message the refusing write path reports</param>
/// <param name="Details">Fields that travel next to the code (names, counts, dates, conflict items)</param>
public sealed record WorkBookingRefusal(string Code, string Message, IReadOnlyDictionary<string, object?> Details);
