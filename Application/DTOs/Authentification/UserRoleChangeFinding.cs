// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Authentification;

/// <summary>
/// One deterministic finding of the user role change advisor: a code the tests pin and a server-built sentence
/// filled only with right names and the account's display name.
/// </summary>
/// <param name="Code">Finding kind</param>
/// <param name="Statement">Factual sentence about the role change</param>
/// <param name="IsBlocking">True when the advice is not to apply the change at all</param>
public sealed record UserRoleChangeFinding(
    UserRoleChangeFindingCode Code,
    string Statement,
    bool IsBlocking);
