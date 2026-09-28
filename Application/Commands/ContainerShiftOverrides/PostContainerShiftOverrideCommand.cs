// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.ContainerShiftOverrides;

public record PostContainerShiftOverrideCommand(Guid ContainerId, ContainerShiftOverrideResource Resource) : IRequest<ContainerShiftOverrideResource>;
