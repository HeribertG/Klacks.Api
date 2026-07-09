// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Query for raw turn goldset candidates extracted from telemetry, ready for human curation.
/// </summary>
/// <param name="Days">How many days of telemetry to look back</param>
/// <param name="Limit">Maximum number of candidates to return</param>

using Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Assistant;

public class GetTurnGoldsetCandidatesQuery : IRequest<List<TurnGoldsetItem>>
{
    public int Days { get; set; }

    public int Limit { get; set; }
}
