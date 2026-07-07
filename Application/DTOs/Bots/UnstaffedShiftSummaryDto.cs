// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Bots;

public record UnstaffedShiftSummaryDto(
    Guid ClientId,
    string ClientName,
    DateOnly StartDate,
    DateOnly EndDate,
    int UnstaffedShiftDayCount);
