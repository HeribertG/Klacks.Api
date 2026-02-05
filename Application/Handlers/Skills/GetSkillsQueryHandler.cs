using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.Skills;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Skills;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Skills;

public class GetSkillsQueryHandler : BaseHandler, IRequestHandler<GetSkillsQuery, IReadOnlyList<SkillDto>>
{
    private readonly ISkillRegistry _skillRegistry;
    private readonly SkillMapper _mapper;

    public GetSkillsQueryHandler(
        ISkillRegistry skillRegistry,
        SkillMapper mapper,
        ILogger<GetSkillsQueryHandler> logger)
        : base(logger)
    {
        _skillRegistry = skillRegistry;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<SkillDto>> Handle(GetSkillsQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(() =>
        {
            _logger.LogInformation("Getting skills for user with {PermissionCount} permissions",
                request.UserPermissions.Count);

            var skills = _skillRegistry.GetSkillsForUser(request.UserPermissions);
            var dtos = _mapper.ToDtos(skills);

            _logger.LogInformation("Returning {SkillCount} skills", dtos.Count);
            return Task.FromResult<IReadOnlyList<SkillDto>>(dtos);
        }, "getting skills", new { PermissionCount = request.UserPermissions.Count });
    }
}
