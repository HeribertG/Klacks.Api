// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.DTOs.PeriodClosing;

/// <summary>
/// Outcome of reopening a period: how many rows changed in total, how many day locks were lifted, and to
/// which lock level the reopened work and break entries went back.
/// </summary>
/// <param name="AffectedCount">Reopened work and break entries plus lifted day locks; what the REST endpoint returns</param>
/// <param name="SealedDayCount">Day locks (SealedDay rows) that were removed</param>
/// <param name="Entries">Reopened work and break entries, split by the lock level they went back to</param>
public sealed record PeriodReopenResult(int AffectedCount, int SealedDayCount, PeriodUnsealCounts Entries);
