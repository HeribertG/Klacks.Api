// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Channel names an escalation stage records as its delivery path. Messenger channels are named by the
/// messaging plugin that delivered them; the inbox is Klacks' own channel and the only one an approval
/// chain uses (Owner decision 2026-09-20: approvals go to inbox and live push, never to a messenger).
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class EscalationDeliveryChannels
{
    public const string Inbox = "inbox";
}
