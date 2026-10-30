// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Text.Json.Serialization;

namespace Klacks.Api.Application.DTOs.PeriodClosing;

/// <summary>
/// The verdict of the period close readiness advisor.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<PeriodCloseVerdict>))]
public enum PeriodCloseVerdict
{
    CloseNow = 0,
    FixErrorsFirst = 1,
    WaitUntilDue = 2,
    NothingToSeal = 3
}
