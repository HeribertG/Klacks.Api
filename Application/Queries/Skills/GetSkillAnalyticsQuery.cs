using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Skills;

namespace Klacks.Api.Application.Queries.Skills;

public record GetSkillAnalyticsQuery(int Days) : IRequest<SkillAnalyticsDto>;
