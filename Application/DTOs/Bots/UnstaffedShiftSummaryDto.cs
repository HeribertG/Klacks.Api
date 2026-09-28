// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Bots;

public record UnstaffedShiftSummaryDto(
    Guid ClientId,
    string ClientName,
    DateOnly StartDate,
    DateOnly EndDate,
    int UnstaffedShiftDayCount);
