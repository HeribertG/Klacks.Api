// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.ContainerShiftOverrides;

public record GetContainerShiftOverridesForRangeQuery(Guid ContainerId, DateOnly FromDate, DateOnly ToDate) : IRequest<List<ContainerShiftOverrideResource>>;
