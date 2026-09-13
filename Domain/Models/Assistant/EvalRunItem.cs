// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One replayed goldset item of one eval run. Without it a run is a single composite number and the two
/// causes behind a miss - the expected tool was never offered, or it was offered and the model picked
/// another - are indistinguishable after the fact. The learning loop reads these rows, so each carries a
/// consumption watermark: a row may justify at most one description proposal. The watermark is per row,
/// not per goldset item - every full run writes fresh rows for the same items, so the ratchet bounds
/// what one run may spend, not what an item may ever cause.
/// </summary>
using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Assistant;

public class EvalRunItem : BaseEntity
{
    public Guid EvalRunId { get; set; }

    public string ItemId { get; set; } = string.Empty;

    public string? Locale { get; set; }

    public string? ExpectedTool { get; set; }

    public string? ChosenTool { get; set; }

    public string ToolsetNamesJson { get; set; } = "[]";

    public bool? RetrievalHit { get; set; }

    public bool? SelectionHit { get; set; }

    public bool Passed { get; set; }

    public int LatencyMs { get; set; }

    public DateTime? LearningConsumedAtUtc { get; set; }
}
