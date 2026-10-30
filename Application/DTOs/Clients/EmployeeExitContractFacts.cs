// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Clients;

/// <summary>
/// The active contract assignments of the employee around the exit date.
/// </summary>
/// <param name="RunningPastExit">Active contracts valid on the exit date that are open-ended or end after it</param>
/// <param name="CoveredOnExitDate">True when at least one active contract is valid on the exit date</param>
/// <param name="LatestEndBeforeExit">End of the latest active contract that ended before the exit date; set only when no contract covers the exit date</param>
public sealed record EmployeeExitContractFacts(
    int RunningPastExit,
    bool CoveredOnExitDate,
    DateOnly? LatestEndBeforeExit);
