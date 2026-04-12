// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.ContainerShiftOverrides;

public record PutContainerShiftOverrideCommand(Guid ContainerId, Guid OverrideId, ContainerShiftOverrideResource Resource) : IRequest<ContainerShiftOverrideResource>;
