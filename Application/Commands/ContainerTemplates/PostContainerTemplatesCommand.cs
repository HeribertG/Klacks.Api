using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Commands.ContainerTemplates;

public record PostContainerTemplatesCommand(Guid ContainerId, List<ContainerTemplateResource> Resources) : IRequest<List<ContainerTemplateResource>>;
