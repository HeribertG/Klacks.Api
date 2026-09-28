// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Bots;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Bots;

public record GetUnstaffedShiftSummaryQuery(string ClientName, DateOnly StartDate, DateOnly EndDate)
    : IRequest<UnstaffedShiftSummaryDto>;
