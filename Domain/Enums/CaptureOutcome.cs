// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Enums;

/// <summary>
/// Terminal outcome of a captured wizard run, resolved after apply. Accepted when the period was sealed,
/// Rejected when the operator declined the scenario, Superseded when a later run replaced it, Expired when
/// no seal happened within the fallback window. Null until an outcome is resolved.
/// </summary>
public enum CaptureOutcome
{
    Accepted = 0,
    Rejected = 1,
    Superseded = 2,
    Expired = 3
}
