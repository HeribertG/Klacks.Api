using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.ContainerTemplates;

public record GetContainerTemplatesQuery(Guid ContainerId) : IRequest<List<ContainerTemplateResource>>;
