// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Macros;

/// <summary>
/// A planned undo together with its dry run on real entries, or the reason it cannot be offered.
/// </summary>
/// <param name="Plan">The plan; its warnings include those the dry run raised</param>
/// <param name="DryRun">The dry run from the current back to the restored macros; null when the plan was refused</param>
/// <param name="Refusal">The plan's refusal, or why a restored macro cannot run; null when the undo can be offered</param>
public record MacroRevertPreview(MacroRevertPlan Plan, MacroDryRunResult? DryRun, string? Refusal);
