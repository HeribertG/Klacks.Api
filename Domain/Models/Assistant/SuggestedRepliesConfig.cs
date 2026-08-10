// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Models.Assistant;

public class SuggestedRepliesConfig
{
    public string SelectionMode { get; set; } = SuggestedReplySelectionModes.Single;
    public string? Prompt { get; set; }
    public List<SuggestedReply> Options { get; set; } = [];

    /// <summary>
    /// Lower bound of the numeric input, used only when SelectionMode is Number. Null leaves the field
    /// unbounded on that side. Carried so a question with a known range ("how many consecutive days?",
    /// 1..31) can be answered in a field that refuses out-of-range values, instead of free text that
    /// costs an extra correction turn.
    /// </summary>
    public decimal? Min { get; set; }

    /// <summary>Upper bound of the numeric input; Number mode only, null means unbounded.</summary>
    public decimal? Max { get; set; }

    /// <summary>
    /// Increment of the numeric input; Number mode only. Null renders a whole-number field, so a
    /// fractional value such as 8.5 hours needs an explicit step (e.g. 0.5).
    /// </summary>
    public decimal? Step { get; set; }
}
