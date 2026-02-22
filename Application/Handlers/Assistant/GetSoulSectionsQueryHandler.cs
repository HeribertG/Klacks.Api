// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class GetSoulSectionsQueryHandler : IRequestHandler<GetSoulSectionsQuery, object>
{
    private readonly IAgentSoulRepository _soulRepository;

    public GetSoulSectionsQueryHandler(IAgentSoulRepository soulRepository)
    {
        _soulRepository = soulRepository;
    }

    public async Task<object> Handle(GetSoulSectionsQuery request, CancellationToken cancellationToken)
    {
        var sections = await _soulRepository.GetActiveSectionsAsync(request.AgentId, cancellationToken);
        return sections.Select(s => new
        {
            s.Id, s.SectionType, s.Content, s.SortOrder,
            s.IsActive, s.Version, s.Source, s.CreateTime
        }).ToList();
    }
}
