// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class GetAgentSkillsQueryHandler : IRequestHandler<GetAgentSkillsQuery, object>
{
    private readonly IAgentSkillRepository _skillRepository;

    public GetAgentSkillsQueryHandler(IAgentSkillRepository skillRepository)
    {
        _skillRepository = skillRepository;
    }

    public async Task<object> Handle(GetAgentSkillsQuery request, CancellationToken cancellationToken)
    {
        var skills = await _skillRepository.GetEnabledAsync(request.AgentId, cancellationToken);
        return skills.Select(s => new
        {
            s.Id, s.Name, s.Description, s.Category,
            s.IsEnabled, s.SortOrder, s.ExecutionType, s.Version
        }).ToList();
    }
}
