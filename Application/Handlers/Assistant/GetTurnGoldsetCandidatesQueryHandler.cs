// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Extracts turn goldset candidates from telemetry via the candidate extractor.
/// </summary>

using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class GetTurnGoldsetCandidatesQueryHandler : IRequestHandler<GetTurnGoldsetCandidatesQuery, List<TurnGoldsetItem>>
{
    private readonly TurnGoldsetCandidateExtractor _extractor;

    public GetTurnGoldsetCandidatesQueryHandler(TurnGoldsetCandidateExtractor extractor)
    {
        _extractor = extractor;
    }

    public async Task<List<TurnGoldsetItem>> Handle(GetTurnGoldsetCandidatesQuery request, CancellationToken cancellationToken)
    {
        var fromDate = DateTime.UtcNow.AddDays(-Math.Max(1, request.Days));
        var items = await _extractor.ExtractAsync(fromDate, Math.Max(1, request.Limit), cancellationToken);
        return items.ToList();
    }
}
