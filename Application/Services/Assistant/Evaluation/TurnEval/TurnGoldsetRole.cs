// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The caller a goldset item is replayed as. Mirrors the three rights tiers of Permissions: Admin, Supervisor
/// (the Authorised role) and Planner (no role, the Planer floor). An unknown value in a goldset file fails
/// deserialization instead of silently degrading to the Planer floor.
/// </summary>

using System.Text.Json.Serialization;

namespace Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TurnGoldsetRole
{
    [JsonStringEnumMemberName("admin")]
    Admin,

    [JsonStringEnumMemberName("supervisor")]
    Supervisor,

    [JsonStringEnumMemberName("planner")]
    Planner
}
