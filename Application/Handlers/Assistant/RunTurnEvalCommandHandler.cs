// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Runs the turn-selection eval for each requested model sequentially and collects the
/// per-model results. Sequential on purpose: parallel runs would hit provider rate
/// limits and share one scoped DbContext.
/// </summary>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class RunTurnEvalCommandHandler : IRequestHandler<RunTurnEvalCommand, List<TurnEvalRunResult>>
{
    private readonly ITurnEvalRunnerService _runnerService;

    public RunTurnEvalCommandHandler(ITurnEvalRunnerService runnerService)
    {
        _runnerService = runnerService;
    }

    public async Task<List<TurnEvalRunResult>> Handle(RunTurnEvalCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Goldset))
        {
            throw new ArgumentException("Goldset must be provided.", nameof(request));
        }

        if (request.ModelIds.Count == 0)
        {
            throw new ArgumentException("At least one model id must be provided.", nameof(request));
        }

        var results = new List<TurnEvalRunResult>(request.ModelIds.Count);
        foreach (var modelId in request.ModelIds)
        {
            var result = await _runnerService.RunAsync(
                request.Goldset, modelId, request.MaxItems, request.UserId, request.UserRights, cancellationToken);
            results.Add(result);
        }

        return results;
    }
}
