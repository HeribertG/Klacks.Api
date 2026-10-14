// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Names of the channels that dispatch a chat turn with rights capped below the owner's own and without a person in
/// front of the screen (SkillExecutionContext.CappedChannel). MCP is a capped channel too but is marked by its access
/// mode (ExternalAgentAccessMode) instead. The skill executor refuses CappedChannelSkillExclusions for every caller that
/// sets the channel; the toolset filter that hides them from the model sits only in ProcessLLMMessageCommandHandler (the
/// non-streaming turn the Slack owner bridge uses), so a new capped source on the streaming path
/// (LLMStreamingOrchestrator) has to carry the channel into its LLMContext as well.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class CappedChannels
{
    public const string SlackOwnerBridge = "slack-owner-bridge";
}
