// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Types of schedule validation messages.
/// </summary>
using System.Text.Json.Serialization;

namespace Klacks.Api.Domain.Enums;

public enum ScheduleValidationType
{
    [JsonStringEnumMemberName("error")]
    Error,

    [JsonStringEnumMemberName("warning")]
    Warning,

    [JsonStringEnumMemberName("info")]
    Info
}
