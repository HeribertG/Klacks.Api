// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The accepted repair of one cover_absence run, ready to be written into its scenario.
/// </summary>
/// <param name="Deltas">Accepted reassignments, swap relocations before their cover hop</param>
/// <param name="WorkIdMap">Clone source work id to scenario clone id</param>
/// <param name="AnalyseToken">Token of the scenario</param>
/// <param name="Report">How the absence reached the run</param>

using Klacks.Api.Domain.Models.Schedules;
using Klacks.ScheduleRecovery.Model;

namespace Klacks.Api.Application.DTOs.Schedules;

public sealed record CoverMaterialisation(
    IReadOnlyList<CellDelta> Deltas,
    IReadOnlyDictionary<Guid, Guid> WorkIdMap,
    Guid AnalyseToken,
    CoverReport Report);
