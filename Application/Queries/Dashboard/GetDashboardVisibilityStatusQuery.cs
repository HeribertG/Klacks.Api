// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Dashboard;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Dashboard;

public record GetDashboardVisibilityStatusQuery() : IRequest<DashboardVisibilityStatusResource>;
