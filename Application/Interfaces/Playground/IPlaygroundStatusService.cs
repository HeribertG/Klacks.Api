// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Playground;
using Klacks.Api.Domain.Models.Playground;

namespace Klacks.Api.Application.Interfaces.Playground;

/// <summary>
/// Answers the Playground status request: whether the token limit is active and, for a caller without a
/// valid device token, a freshly minted one.
/// </summary>
public interface IPlaygroundStatusService
{
    /// <param name="visitor">The visitor the middleware built for this request; null when the mode is off</param>
    PlaygroundStatusResource GetStatus(PlaygroundVisitor? visitor);
}
