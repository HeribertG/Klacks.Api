// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Assistant;

/// <summary>
/// Body of the 202 answer to a turn stop request. The client does not read it; the status code carries
/// the meaning, the flag only keeps the body a valid JSON object.
/// </summary>
public class CancelTurnResponse
{
    public bool Accepted { get; init; }
}
