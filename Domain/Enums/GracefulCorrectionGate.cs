// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Enums;

public enum GracefulCorrectionGate
{
    /// <summary>Every gate passed: the message corrects the previous turn.</summary>
    Passed = 0,

    /// <summary>G0: no usable previous action - none stored, superseded, or older than the window.</summary>
    Anchor = 1,

    /// <summary>G1: a recipe is paused or engaging, and owns the correction path of its own.</summary>
    ActiveRecipe = 2,

    /// <summary>G2: no negation/correction token from the 25-language vocabulary.</summary>
    Signal = 3,

    /// <summary>G3: a bare negation, which declines rather than corrects.</summary>
    BareNegation = 4,

    /// <summary>G4: the message reads as an independent question.</summary>
    TopicSwitch = 5,

    /// <summary>G5: the message already guarantees a skill on its own, so it is a new request.</summary>
    RoutesAlone = 6
}
