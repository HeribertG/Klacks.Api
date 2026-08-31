// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Acknowledges one of the user's own proactive inbox messages, which is the only stop truth for the
/// reminder loop of package F ("repeat until acknowledged"): the repository stamps AcknowledgedAtUtc on
/// first acknowledgement and clears NextReminderAtUtc. Returns false when the row does not exist or
/// belongs to a different user, so the caller can answer with not found without leaking foreign rows.
/// </summary>
/// <param name="dispatchRepository">Persistence of the proactive trigger dispatch rows.</param>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class AcknowledgeProactiveMessageCommandHandler : IRequestHandler<AcknowledgeProactiveMessageCommand, bool>
{
    private readonly IProactiveTriggerDispatchRepository _dispatchRepository;

    public AcknowledgeProactiveMessageCommandHandler(IProactiveTriggerDispatchRepository dispatchRepository)
    {
        _dispatchRepository = dispatchRepository;
    }

    public async Task<bool> Handle(AcknowledgeProactiveMessageCommand request, CancellationToken cancellationToken)
    {
        return await _dispatchRepository.AcknowledgeAsync(request.Id, request.UserId, cancellationToken);
    }
}
