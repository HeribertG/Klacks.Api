// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class UpsertSoulSectionCommandHandler : IRequestHandler<UpsertSoulSectionCommand, object>
{
    private readonly IAgentSoulRepository _soulRepository;

    public UpsertSoulSectionCommandHandler(IAgentSoulRepository soulRepository)
    {
        _soulRepository = soulRepository;
    }

    public async Task<object> Handle(UpsertSoulSectionCommand request, CancellationToken cancellationToken)
    {
        await _soulRepository.UpsertSectionAsync(
            request.AgentId, request.SectionType, request.Content,
            request.SortOrder ?? SoulSectionTypes.GetDefaultSortOrder(request.SectionType),
            source: null, changedBy: request.UserId, cancellationToken: cancellationToken);

        return new { AgentId = request.AgentId, SectionType = request.SectionType };
    }
}
