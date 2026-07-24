// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Counts a user's unread proactive inbox messages for the assistant badge.
/// </summary>
/// <param name="dispatchRepository">Persistence of the proactive trigger dispatch rows.</param>

using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class GetProactiveUnreadCountQueryHandler : IRequestHandler<GetProactiveUnreadCountQuery, int>
{
    private readonly IProactiveTriggerDispatchRepository _dispatchRepository;

    public GetProactiveUnreadCountQueryHandler(IProactiveTriggerDispatchRepository dispatchRepository)
    {
        _dispatchRepository = dispatchRepository;
    }

    public async Task<int> Handle(GetProactiveUnreadCountQuery request, CancellationToken cancellationToken)
    {
        return await _dispatchRepository.CountUnreadAsync(request.UserId, cancellationToken);
    }
}
