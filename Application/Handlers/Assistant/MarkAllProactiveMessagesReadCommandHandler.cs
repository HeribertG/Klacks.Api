// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Marks all unread proactive inbox messages of the requesting user as read.
/// </summary>
/// <param name="dispatchRepository">Persistence of the proactive trigger dispatch rows.</param>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class MarkAllProactiveMessagesReadCommandHandler : IRequestHandler<MarkAllProactiveMessagesReadCommand>
{
    private readonly IProactiveTriggerDispatchRepository _dispatchRepository;

    public MarkAllProactiveMessagesReadCommandHandler(IProactiveTriggerDispatchRepository dispatchRepository)
    {
        _dispatchRepository = dispatchRepository;
    }

    public async Task<Unit> Handle(MarkAllProactiveMessagesReadCommand request, CancellationToken cancellationToken)
    {
        await _dispatchRepository.MarkAllReadAsync(request.UserId, cancellationToken);
        return Unit.Value;
    }
}
