// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Origin of a pending confirmation, stored with its token. Chat covers the Klacks assistant chat and every path that
/// runs through it (Slack bridge, messenger inbound); Mcp is a tool call of an external agent over the MCP endpoint.
/// The chat only ever redeems, resurfaces and forces tokens of the Chat channel, so a token an external agent caused
/// can neither be redeemed by a later unrelated "ja" in the chat nor be mistaken for the user's own question.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class PendingConfirmationChannels
{
    public const string Chat = "chat";

    public const string Mcp = "mcp";

    public const int MaxLength = 16;
}
