// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Schedules;

public record AcquireContainerLockCommand(string ResourceType, Guid ResourceId) : IRequest<ContainerLockResource>;

public record HeartbeatContainerLockCommand(Guid LockId) : IRequest<ContainerLockResource>;

public record ReleaseContainerLockCommand(Guid LockId) : IRequest<bool>;
