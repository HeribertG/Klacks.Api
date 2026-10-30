// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// One deterministic finding of the container bundling advice: a code the tests pin and a server-built sentence filled
/// only with counts and minutes.
/// </summary>
/// <param name="Code">Finding kind</param>
/// <param name="Statement">Factual sentence</param>
/// <param name="IsBlocking">True when no tour can be judged or the bundling would refuse</param>
public sealed record ContainerBundlingFinding(ContainerBundlingFindingCode Code, string Statement, bool IsBlocking);
