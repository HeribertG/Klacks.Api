// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Inbound;

/// <summary>
/// Channel-neutral view of one inbound message (email or messenger), threaded through the shared
/// intent-analysis/action-orchestration/notification kernel. Built by the channel-specific adapter
/// (EmailPollingBackgroundService, MessengerIntentObserver) right before calling into the kernel.
/// </summary>
/// <param name="SourceId">Id of the underlying ReceivedEmail or messaging-plugin Message row</param>
/// <param name="SourceKind">Which channel this came in on</param>
/// <param name="Channel">Human-readable channel label for audit/logging, e.g. "Email" or "Messenger:Telegram"</param>
/// <param name="SenderDisplay">Best available human-readable sender label</param>
/// <param name="Subject">Email subject line; null for messenger (no such concept)</param>
/// <param name="Body">Message text the LLM classifies</param>
/// <param name="ReceivedAt">UTC instant the message was received</param>
public sealed record InboundSource(
    Guid SourceId,
    InboundSourceKind SourceKind,
    string Channel,
    string SenderDisplay,
    string? Subject,
    string Body,
    DateTime ReceivedAt);
