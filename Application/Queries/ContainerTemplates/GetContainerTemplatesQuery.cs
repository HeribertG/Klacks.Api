using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Queries.ContainerTemplates;

public record GetContainerTemplatesQuery(Guid ContainerId) : IRequest<List<ContainerTemplateResource>>;
