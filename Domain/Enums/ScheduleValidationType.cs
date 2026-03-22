// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
