// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The decisions both chat loops need before their first provider call, produced by ONE call so the
/// streaming and the non-streaming path cannot drift apart: the recipe plan (null when none is active),
/// whether an outstanding confirmation must be forced this turn together with the tool to narrow to,
/// and the volatile note that belongs to that decision.
///
/// The graceful correction is deliberately NOT a field here. Its outcome depends on the assembled
/// toolset, which does not exist when PrepareAsync runs, and it is already on the LLMContext by then -
/// carrying a copy would be state that is written once and read never. See the plan's deviation 8.
/// </summary>

using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record TurnPreparation(
    RecipeExecutionPlan? Plan,
    bool ForceConfirm,
    LLMFunction? ConfirmFunction,
    string? VolatileNote);
