using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Commands.ContainerTemplates;

public record DeleteContainerTemplatesCommand(Guid ContainerId) : IRequest<List<ContainerTemplateResource>>;
