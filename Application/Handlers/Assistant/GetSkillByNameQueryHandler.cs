using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Assistant;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Assistant;

public class GetSkillByNameQueryHandler : BaseHandler, IRequestHandler<GetSkillByNameQuery, SkillDto?>
{
    private readonly ISkillRegistry _skillRegistry;
    private readonly SkillMapper _mapper;

    public GetSkillByNameQueryHandler(
        ISkillRegistry skillRegistry,
        SkillMapper mapper,
        ILogger<GetSkillByNameQueryHandler> logger)
        : base(logger)
    {
        _skillRegistry = skillRegistry;
        _mapper = mapper;
    }

    public async Task<SkillDto?> Handle(GetSkillByNameQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(() =>
        {
            _logger.LogInformation("Getting skill by name: {SkillName}", request.Name);

            var skill = _skillRegistry.GetSkillByName(request.Name);
            if (skill == null)
            {
                _logger.LogWarning("Skill not found: {SkillName}", request.Name);
                return Task.FromResult<SkillDto?>(null);
            }

            var dto = _mapper.ToDto(skill);
            return Task.FromResult<SkillDto?>(dto);
        }, "getting skill by name", new { SkillName = request.Name });
    }
}
