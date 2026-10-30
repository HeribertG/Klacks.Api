// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Clients;

/// <summary>
/// One deterministic finding of the employee exit advisor: a code the tests pin and a server-built sentence filled only
/// with counts, dates and the employee's display name.
/// </summary>
/// <param name="Code">Finding kind</param>
/// <param name="Statement">Factual sentence about the exit</param>
/// <param name="IsBlocking">True when the exit cannot be recorded as asked (the recording skill would refuse it)</param>
public sealed record EmployeeExitFinding(
    EmployeeExitFindingCode Code,
    string Statement,
    bool IsBlocking);
