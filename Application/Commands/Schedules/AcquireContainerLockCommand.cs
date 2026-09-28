// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Schedules;

public record AcquireContainerLockCommand(string ResourceType, Guid ResourceId, string InstanceId) : IRequest<ContainerLockResource>;

public record HeartbeatContainerLockCommand(Guid LockId) : IRequest<ContainerLockResource>;

public record ReleaseContainerLockCommand(Guid LockId) : IRequest<bool>;
