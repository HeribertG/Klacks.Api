// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Grouping;

public sealed record GroupingShiftRecord(
    Guid Id,
    string DisplayName,
    TimeOnly Start,
    TimeOnly End,
    int Quantity,
    Guid? OriginalId = null,
    Guid? CustomerId = null,
    double? Latitude = null,
    double? Longitude = null,
    int SumEmployees = 1);
