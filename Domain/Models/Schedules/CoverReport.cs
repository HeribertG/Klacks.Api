// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// How an absence reached the cover run.
/// </summary>
/// <param name="Source">Path that started the run</param>
/// <param name="ReportedAtUtc">Instant the absence was reported</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Schedules;

public sealed record CoverReport(ReplacementRequestSource Source, DateTime ReportedAtUtc);
