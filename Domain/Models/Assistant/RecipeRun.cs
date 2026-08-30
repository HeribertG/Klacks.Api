// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Assistant;

/// <summary>
/// One run of a recipe forcing plan (W1.5): the denominator of the recipe funnel
/// (gestartet → vollendet/abgebrochen/verfallen). A run spans all turns of a guided flow — the row is
/// created when the plan engages, gets the current turn id appended on every resume, and is closed
/// when the plan completes, aborts, or the pending-store TTL lapses.
/// </summary>
public class RecipeRun : BaseEntity
{
    public required string RecipeName { get; set; }

    public required Guid UserId { get; set; }

    public required string ConversationId { get; set; }

    public RecipeRunStatus Status { get; set; }

    /// <summary>Highest step index the plan reached (0-based, as RecipeExecutionPlan.StepIndex).</summary>
    public int LastStep { get; set; }

    /// <summary>JSON array of the turn ids this run was active in (llm_usages.id).</summary>
    public string TurnIdsJson { get; set; } = "[]";

    /// <summary>Reason when Status is Aborted (autonomy gate hold, user cancellation, ambiguity).</summary>
    public string? AbortReason { get; set; }
}
