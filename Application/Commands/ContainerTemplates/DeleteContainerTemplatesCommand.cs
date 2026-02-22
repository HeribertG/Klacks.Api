// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.ContainerTemplates;

public record DeleteContainerTemplatesCommand(Guid ContainerId) : IRequest<List<ContainerTemplateResource>>;
