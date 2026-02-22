using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class GetAgentByIdQueryHandler : IRequestHandler<GetAgentByIdQuery, object?>
{
    private readonly IAgentRepository _agentRepository;
    private readonly IAgentSoulRepository _soulRepository;
    private readonly IAgentSkillRepository _skillRepository;

    public GetAgentByIdQueryHandler(
        IAgentRepository agentRepository,
        IAgentSoulRepository soulRepository,
        IAgentSkillRepository skillRepository)
    {
        _agentRepository = agentRepository;
        _soulRepository = soulRepository;
        _skillRepository = skillRepository;
    }

    public async Task<object?> Handle(GetAgentByIdQuery request, CancellationToken cancellationToken)
    {
        var agent = await _agentRepository.GetByIdAsync(request.Id, cancellationToken);
        if (agent == null) return null;

        var sections = await _soulRepository.GetActiveSectionsAsync(request.Id, cancellationToken);
        var skills = await _skillRepository.GetEnabledAsync(request.Id, cancellationToken);

        return new
        {
            agent.Id, agent.Name, agent.DisplayName, agent.Description,
            agent.IsActive, agent.IsDefault, agent.CreateTime,
            SoulSections = sections.Select(s => new { s.Id, s.SectionType, s.SortOrder, s.Version }),
            SkillCount = skills.Count
        };
    }
}
