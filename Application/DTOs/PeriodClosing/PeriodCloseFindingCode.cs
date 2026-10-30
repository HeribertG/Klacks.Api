// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Text.Json.Serialization;

namespace Klacks.Api.Application.DTOs.PeriodClosing;

/// <summary>
/// The deterministic findings the period close readiness advisor can raise.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<PeriodCloseFindingCode>))]
public enum PeriodCloseFindingCode
{
    AlreadySealed = 0,
    WorkOnlyOnChildGroups = 1,
    NoWorkInPeriod = 2,
    NotDueYet = 3,
    OpenErrors = 4,
    Warnings = 5,
    UnsealedDaysWithWork = 6,
    PartiallySealed = 7,
    ReopenedBefore = 8,
    NoCloseLagStored = 9
}
