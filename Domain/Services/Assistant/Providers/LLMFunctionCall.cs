// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Text.Json.Serialization;
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

    /// <summary>
    /// True when the model called a skill that was not part of the turn's toolset. Purely observational: the call
    /// is flagged in both ToolsetMembershipModes, and in Observe mode it still ran with its real outcome.
    /// </summary>
    public bool IsOutsideToolset { get; set; }

    /// <summary>
    /// True when the call was rejected without running because its skill was not part of the turn's toolset
    /// (ToolsetMembershipMode.Enforce). Like IsRejectedRepeat it sets Success=false without being a skill
    /// failure, so failure-driven consumers and the record of what the turn executed must exclude it.
    /// </summary>
    public bool IsRejectedOutsideToolset { get; set; }

    /// <summary>
    /// True when the call was rejected without running because a recipe had already completed in this turn and
    /// the skill is side-effecting (ReadOnlyRecipeWriteGuard). Like IsRejectedRepeat it sets Success=false without
    /// being a skill failure, so failure-driven consumers and the record of what the turn executed must exclude it.
    /// </summary>
    public bool IsRejectedByReadOnlyRecipeGuard { get; set; }

    /// <summary>
    /// True when the loop rejected the call without running it, whatever the reason: a repeat of a write, a skill
    /// outside the toolset, or a write after a completed recipe. Such a call stored nothing and is no skill failure;
    /// its result text is addressed to the model and must never reach the user. A call skipped by the user's stop
    /// is not part of this: it belongs to a stopped turn, which never reaches the consumers of this property.
    /// </summary>
    [JsonIgnore]
    public bool IsRejectedWithoutRunning =>
        IsRejectedRepeat || IsRejectedOutsideToolset || IsRejectedByReadOnlyRecipeGuard;

    /// <summary>
    /// True when the skill's result was a navigation: the server only worked out where to go, and the page is
    /// opened by the client once it receives the metadata event. Such a call changed nothing on the server, so
    /// the record of what a stopped turn did leaves it out.
    /// </summary>
    public bool IsNavigation { get; set; }
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

    /// <summary>
    /// The skill a wrapper call (confirm_pending_action) actually ran. FunctionName stays the wrapper's own name,
    /// so the correction and undo paths keep treating the call as the wrapper. Null for every other call.
    /// </summary>
    public string? ReplayedSkillName { get; set; }

    /// <summary>
    /// The one-time confirmation token the skill issued when it held the call (RequiresConfirmation), null for every
    /// other call. Server-internal: it lets a later step address exactly the confirmation this call created instead of
    /// "the user's latest one", and it is never serialized.
    /// </summary>
    [JsonIgnore]
    public string? IssuedConfirmationToken { get; set; }
}