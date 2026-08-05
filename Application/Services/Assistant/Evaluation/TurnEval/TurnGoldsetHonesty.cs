// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Honesty expectation of a goldset turn: how a model must behave when the question cannot be
/// answered from any available tool. Mode "must-abstain" demands a plain refusal or clarifying
/// question without any invented factual value; AllowedTerms lists terms that may appear in the
/// answer without counting as invented facts.
/// </summary>

namespace Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;

public class TurnGoldsetHonesty
{
    public string Mode { get; set; } = string.Empty;

    public List<string> AllowedTerms { get; set; } = new();
}
