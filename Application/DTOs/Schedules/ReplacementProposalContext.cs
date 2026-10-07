// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The facts shared by every proposal of one cover_absence run, handed to the request book together with the
/// covered slots.
/// </summary>
/// <param name="AbsentClientId">Employee who is absent</param>
/// <param name="GroupId">Group the absence was covered in (context only)</param>
/// <param name="AbsenceId">Absence type (the "why")</param>
/// <param name="AnalyseToken">Token of the scenario holding the proposals</param>
/// <param name="Source">Path that started the run</param>
/// <param name="ReportedAtUtc">When the absence was reported; null means now</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Schedules;

public sealed record ReplacementProposalContext(
    Guid AbsentClientId,
    Guid GroupId,
    Guid AbsenceId,
    Guid AnalyseToken,
    ReplacementRequestSource Source,
    DateTime? ReportedAtUtc);
