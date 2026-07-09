// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Command to run a turn-selection goldset against one or more models sequentially.
/// </summary>
/// <param name="Goldset">Name of the goldset file to evaluate</param>
/// <param name="ModelIds">Models to evaluate, one EvalRun is persisted per model</param>
/// <param name="MaxItems">Optional cost cap: only the first N goldset items are replayed</param>
/// <param name="UserId">Id of the admin triggering the run, used for toolset assembly</param>
/// <param name="UserRights">Rights of the triggering admin, used for toolset assembly</param>

using Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant;

public class RunTurnEvalCommand : IRequest<List<TurnEvalRunResult>>
{
    public string Goldset { get; set; } = string.Empty;

    public List<string> ModelIds { get; set; } = new();

    public int? MaxItems { get; set; }

    public string UserId { get; set; } = string.Empty;

    public List<string> UserRights { get; set; } = new();
}
