using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.ContainerTemplates;

public record PutContainerTemplatesCommand(Guid ContainerId, List<ContainerTemplateResource> Resources) : IRequest<List<ContainerTemplateResource>>;
