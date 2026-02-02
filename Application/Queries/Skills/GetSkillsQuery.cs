using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Skills;

namespace Klacks.Api.Application.Queries.Skills;

public record GetSkillsQuery(List<string> UserPermissions) : IRequest<IReadOnlyList<SkillDto>>;
