// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.ContainerShiftOverrides;

public record GetContainerShiftOverrideQuery(Guid ContainerId, DateOnly Date) : IRequest<ContainerShiftOverrideResource?>;
