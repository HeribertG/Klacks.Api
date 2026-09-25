// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Services.Assistant.Providers;

public class LLMFunctionCall
{
    public string FunctionName { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string? Result { get; set; }
    public bool Success { get; set; } = true;
    public bool RequiresConfirmation { get; set; }

    /// <summary>
    /// True when the loop rejected this call as a repeat of a write skill that already ran earlier
    /// in the same turn. Like RequiresConfirmation this sets Success=false without being a real
    /// skill failure, so failure-driven consumers (reflection, grounding skip, last-error notice)
    /// must exclude it.
    /// </summary>
    public bool IsRejectedRepeat { get; set; }

    /// <summary>
    /// True when the call did not run because the user stopped the turn: it was skipped before it started,
    /// or it is a read that the stop cut short. Like RequiresConfirmation and IsRejectedRepeat it sets
    /// Success=false without being a skill failure, so failure-driven consumers (reflection, grounding
    /// skip, last-error notice) and the record of what the turn executed must exclude it.
    /// </summary>
    public bool SkippedByStop { get; set; }
    public string? UiActionSteps { get; set; }
    public Guid? UiActionTrackingId { get; set; }
    public LLMFunctionResultKind ResultKind { get; set; }
    public List<string> DataJson { get; set; } = new();

    /// <summary>
    /// True when the executed skill result carries text authored outside this system, including content
    /// relayed by a wrapper skill under its own name. The tool-result formatter frames such results as
    /// untrusted in addition to the skills listed by name in UntrustedSkillOutputs.
    /// </summary>
    public bool ContainsExternalContent { get; set; }
}