// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Marks exactly the listed proactive inbox messages of the requesting user as read. The client
/// sends the ids it actually rendered, so rows beyond the page it fetched keep their unread state.
/// </summary>
/// <param name="dispatchRepository">Persistence of the proactive trigger dispatch rows.</param>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class MarkProactiveMessagesReadCommandHandler : IRequestHandler<MarkProactiveMessagesReadCommand>
{
    private readonly IProactiveTriggerDispatchRepository _dispatchRepository;

    public MarkProactiveMessagesReadCommandHandler(IProactiveTriggerDispatchRepository dispatchRepository)
    {
        _dispatchRepository = dispatchRepository;
    }

    public async Task<Unit> Handle(MarkProactiveMessagesReadCommand request, CancellationToken cancellationToken)
    {
        await _dispatchRepository.MarkManyReadAsync(request.Ids, request.UserId, cancellationToken);
        return Unit.Value;
    }
}
