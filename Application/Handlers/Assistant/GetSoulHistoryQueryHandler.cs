using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class GetSoulHistoryQueryHandler : IRequestHandler<GetSoulHistoryQuery, object>
{
    private readonly IAgentSoulRepository _soulRepository;

    public GetSoulHistoryQueryHandler(IAgentSoulRepository soulRepository)
    {
        _soulRepository = soulRepository;
    }

    public async Task<object> Handle(GetSoulHistoryQuery request, CancellationToken cancellationToken)
    {
        var history = await _soulRepository.GetHistoryAsync(request.AgentId, request.Limit, cancellationToken);
        return history.Select(h => new
        {
            h.Id, h.SectionType, h.ContentBefore, h.ContentAfter,
            h.Version, h.ChangeType, h.ChangedBy, h.CreateTime
        }).ToList();
    }
}
