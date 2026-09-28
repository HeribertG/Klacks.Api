// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Answer of POST learning/mark-exported.
/// </summary>
/// <param name="Marked">How many proposals moved from gate_passed to exported</param>
/// <param name="Skipped">Ids that were unknown, not gate-passed or not a description proposal</param>
namespace Klacks.Api.Application.DTOs.Assistant.Learning;

public sealed record MarkProposalsExportedResult(int Marked, IReadOnlyList<Guid> Skipped);
