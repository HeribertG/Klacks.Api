using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class GetAgentSessionMessagesQueryHandler : IRequestHandler<GetAgentSessionMessagesQuery, object>
{
    private readonly IAgentSessionRepository _sessionRepository;

    public GetAgentSessionMessagesQueryHandler(IAgentSessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public async Task<object> Handle(GetAgentSessionMessagesQuery request, CancellationToken cancellationToken)
    {
        var messages = await _sessionRepository.GetActiveMessagesAsync(request.SessionId, cancellationToken);
        return messages.Select(m => new
        {
            m.Id, m.Role, m.Content, m.TokenCount,
            m.ModelId, m.FunctionCalls, m.CreateTime
        }).ToList();
    }
}
