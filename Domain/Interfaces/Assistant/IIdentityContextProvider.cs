// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Provides the cached identity prompt (global rules + soul sections) for an agent.
/// </summary>
/// <param name="agentId">The agent whose identity context to build</param>
/// <param name="language">UI language code for template variable resolution</param>
/// <param name="suppressTextOnlyAffordances">When true (hands-free voice mode), omits the text-only affordance rules (SUGGESTION_FORMAT follow-up chips and SUGGESTED_REPLIES_FORMAT interactive/date-picker chips) — neither can be tapped while speaking and generating them delays the spoken turn — and includes the VOICE_STYLE rule so the answer is composed for the ear. When false (text mode), the affordance rules are kept and VOICE_STYLE is omitted. Workflow-discipline rules (GUIDED_WORKFLOW) are kept in both modes.</param>

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IIdentityContextProvider
{
    Task<string> GetIdentityPromptAsync(Guid agentId, string? language, bool suppressTextOnlyAffordances = false, CancellationToken cancellationToken = default);
}
