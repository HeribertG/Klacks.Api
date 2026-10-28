// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Answer of the anonymous Playground status endpoint.
/// </summary>
/// <param name="LimitActive">True when the daily token limit applies to this instance</param>
/// <param name="DeviceToken">Newly minted device token, null when the caller already sent a valid one or the limit is off</param>

using System.Text.Json.Serialization;

namespace Klacks.Api.Application.DTOs.Playground;

public sealed record PlaygroundStatusResource(
    [property: JsonPropertyName("limitActive")] bool LimitActive,
    [property: JsonPropertyName("deviceToken")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    string? DeviceToken);
