// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Services.Schedules;

/// <summary>
/// Company-local wall-clock interval of a work that a substitute took over.
/// </summary>
/// <param name="Start">Start of the handed-over time</param>
/// <param name="End">End of the handed-over time</param>
public sealed record ReplacementInterval(DateTime Start, DateTime End);
