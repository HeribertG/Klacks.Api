// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Output of ICorrectionTurnPreparer.PrepareAsync: everything a chat entry point needs from the
/// correction/toolset pipeline before it can build its LLMContext.
/// </summary>
/// <param name="Toolset">The turn's assembled skill toolset, correction-composite already folded in.</param>
/// <param name="Correction">Non-null when a correction was detected and completed; carries the note, the
/// clarification reply if one was asked, and the undo invocation if one was resolved.</param>
/// <param name="UndoWasHeld">True when Correction.Undo was successfully registered as a one-time token.</param>
namespace Klacks.Api.Domain.Models.Assistant;

public sealed record CorrectionTurnPreparation(
    SkillToolsetResult Toolset,
    GracefulCorrectionOutcome? Correction,
    bool UndoWasHeld);
