// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Returns all pending skill-description proposals up to the requested limit.
/// </summary>

using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Assistant;

public class GetPendingSkillChangesQueryHandler
    : BaseHandler, IRequestHandler<GetPendingSkillChangesQuery, IReadOnlyList<ProposedSkillChange>>
{
    private readonly IProposedSkillChangeRepository _repository;

    public GetPendingSkillChangesQueryHandler(
        IProposedSkillChangeRepository repository,
        ILogger<GetPendingSkillChangesQueryHandler> logger)
        : base(logger)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<ProposedSkillChange>> Handle(
        GetPendingSkillChangesQuery request,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync(
            () => _repository.GetPendingAsync(request.Limit, cancellationToken),
            "get pending skill changes",
            new { request.Limit });
    }
}
