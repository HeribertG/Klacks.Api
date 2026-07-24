// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Marks one of the user's own proactive inbox messages as read. Returns false when the row does
/// not exist or belongs to a different user, so the caller can answer with not found without
/// leaking foreign rows.
/// </summary>
/// <param name="dispatchRepository">Persistence of the proactive trigger dispatch rows.</param>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class MarkProactiveMessageReadCommandHandler : IRequestHandler<MarkProactiveMessageReadCommand, bool>
{
    private readonly IProactiveTriggerDispatchRepository _dispatchRepository;

    public MarkProactiveMessageReadCommandHandler(IProactiveTriggerDispatchRepository dispatchRepository)
    {
        _dispatchRepository = dispatchRepository;
    }

    public async Task<bool> Handle(MarkProactiveMessageReadCommand request, CancellationToken cancellationToken)
    {
        return await _dispatchRepository.MarkReadAsync(request.Id, request.UserId, cancellationToken);
    }
}
