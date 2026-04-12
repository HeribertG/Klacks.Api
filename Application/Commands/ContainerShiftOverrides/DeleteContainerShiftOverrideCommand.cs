// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.ContainerShiftOverrides;

public record DeleteContainerShiftOverrideCommand(Guid ContainerId, Guid OverrideId) : IRequest<bool>;
