// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reads the per-user autonomy level, falling back to the system default when unset.
/// </summary>
/// <param name="repository">Per-user autonomy preference storage.</param>

using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class GetAutonomyLevelQueryHandler : IRequestHandler<GetAutonomyLevelQuery, AutonomyLevel>
{
    private readonly IAgentAutonomyPreferenceRepository _repository;

    public GetAutonomyLevelQueryHandler(IAgentAutonomyPreferenceRepository repository)
    {
        _repository = repository;
    }

    public async Task<AutonomyLevel> Handle(GetAutonomyLevelQuery request, CancellationToken cancellationToken)
    {
        var row = await _repository.GetAsync(request.UserId.ToString(), cancellationToken);
        return row?.Level ?? AutonomyDefaults.DefaultLevel;
    }
}
