// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Command to update all children of a container work (Savebar pattern - full replace).
/// </summary>
/// <param name="WorkId">The parent container work ID</param>
/// <param name="Resource">The updated children collection</param>
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Works;

public record UpdateContainerWorkChildrenCommand(Guid WorkId, UpdateContainerWorkChildrenResource Resource) : IRequest<ContainerWorkChildrenResource>;
