// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Constants;

public static class CorrectionTypes
{
    public const string None = "none";
    public const string WrongSkill = "wrong_skill";
    public const string WrongParam = "wrong_param";
    public const string RepeatedRequest = "repeated_request";
    public const string NoneNeeded = "none_needed";
    public const string Implicit = "implicit";

    /// <summary>
    /// The user corrected the turn AND the graceful-correction path re-routed it in the same turn (TP1).
    /// Distinct from Implicit, which means the correction was recorded but the turn that followed it was
    /// routed like any other: a re-routed correction is evidence about the ROUTING, an un-rerouted one is
    /// evidence about the skill description, and the sharpening loop must not confuse the two.
    /// </summary>
    public const string GracefulRerouted = "graceful_rerouted";
}
