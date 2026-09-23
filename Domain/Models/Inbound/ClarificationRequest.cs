// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Inbound;

/// <summary>
/// Everything the clarification dialog needs to know about one inbound message, built by the channel
/// adapter (EmailPollingBackgroundService, MessengerIntentProcessor) after the sender was resolved to a
/// client.
/// </summary>
/// <param name="ClientId">Client the message was resolved to</param>
/// <param name="ClientType">Employee, extern employee or customer</param>
/// <param name="Source">The channel-neutral message</param>
/// <param name="ReplyChannel">Delivery channel for a question: "Email" or a messenger type name such as "Telegram"</param>
/// <param name="SenderAddress">Sender as the channel reported it (mail address or messenger id); only used to look up the stored contact, never as recipient</param>
/// <param name="EmailThread">Mail threading headers; null for messenger messages</param>
public sealed record ClarificationRequest(
    Guid ClientId,
    EntityTypeEnum ClientType,
    InboundSource Source,
    string ReplyChannel,
    string SenderAddress,
    ClarificationEmailThread? EmailThread);
