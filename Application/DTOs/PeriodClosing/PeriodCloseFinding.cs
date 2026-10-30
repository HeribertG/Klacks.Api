// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.PeriodClosing;

/// <summary>
/// One deterministic finding of the period close readiness advisor: a code the tests pin and a server-built sentence.
/// </summary>
/// <param name="Code">Finding kind</param>
/// <param name="Statement">Factual sentence about the period</param>
/// <param name="IsBlocking">True when a person closing the period would be refused, which only open errors do; a verdict other than CloseNow can also come from the policy of the automatic close</param>
public sealed record PeriodCloseFinding(
    PeriodCloseFindingCode Code,
    string Statement,
    bool IsBlocking);
