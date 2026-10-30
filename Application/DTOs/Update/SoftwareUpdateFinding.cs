// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Update;

/// <summary>
/// One deterministic finding of the software update advisor: a code the tests pin and a server-built sentence filled only
/// with versions, counts and enum names.
/// </summary>
/// <param name="Code">Finding kind</param>
/// <param name="Statement">Factual sentence about the update</param>
/// <param name="IsBlocking">True when the advice is not to start the update now</param>
public sealed record SoftwareUpdateFinding(
    SoftwareUpdateFindingCode Code,
    string Statement,
    bool IsBlocking);
