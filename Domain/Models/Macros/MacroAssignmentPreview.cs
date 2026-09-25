// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Macros;

/// <summary>
/// A planned macro switch together with its dry run on real entries, or the reason it cannot be offered.
/// </summary>
/// <param name="Plan">The plan; its warnings include those the dry run raised</param>
/// <param name="DryRun">The dry run over the whole cut group; null when the plan was refused</param>
/// <param name="Refusal">The plan's refusal, or why the new macro cannot run; null when the switch can be offered</param>
public record MacroAssignmentPreview(MacroAssignmentPlan Plan, MacroDryRunResult? DryRun, string? Refusal);
