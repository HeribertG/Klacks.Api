// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// Body of the 409 answer when the identical autofill run is already going, so the client can attach to
/// the running job instead of starting a competing one.
/// </summary>
/// <param name="Code">Machine-readable error code.</param>
/// <param name="RunningJobId">Job that already holds the lock.</param>
/// <param name="Message">Human-readable reason.</param>
public sealed record AutofillRunConflictResponse(string Code, Guid RunningJobId, string Message);
