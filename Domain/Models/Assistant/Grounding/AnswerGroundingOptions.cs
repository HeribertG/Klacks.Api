// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Runtime configuration of the answer grounding evaluator, bound from the
/// Assistant:AnswerGroundingMode configuration key at startup.
/// </summary>
/// <param name="Mode">Off (default), Shadow (telemetry only) or Active.</param>

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Models.Assistant.Grounding;

public sealed record AnswerGroundingOptions(string Mode)
{
    public static AnswerGroundingOptions Off { get; } = new(AnswerGroundingModes.Off);

    public bool IsEnabled => !string.Equals(Mode, AnswerGroundingModes.Off, StringComparison.OrdinalIgnoreCase);
}
