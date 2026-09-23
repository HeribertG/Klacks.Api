// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Sends a clarification question privately over the messenger the employee used. The target is the
/// employee's stored personal contact of that messenger type (resolved and checked by
/// IClientMessengerReplyChannel), never the chat or channel the message came from, so a message posted
/// into a shared channel still gets a private question. No personal contact means no target.
/// </summary>
/// <param name="replyChannel">Core-owned bridge to the messaging plugin</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Inbound;
using Klacks.Api.Domain.Models.Inbound;

namespace Klacks.Api.Infrastructure.Inbound;

public sealed class MessengerReplySender : IInboundReplySender
{
    private readonly IClientMessengerReplyChannel _replyChannel;

    public MessengerReplySender(IClientMessengerReplyChannel replyChannel)
    {
        _replyChannel = replyChannel;
    }

    public InboundSourceKind SourceKind => InboundSourceKind.Messenger;

    public async Task<InboundReplyTarget?> ResolveTargetAsync(ClarificationRequest request, CancellationToken cancellationToken = default)
    {
        var recipient = await _replyChannel.ResolvePersonalRecipientAsync(request.ClientId, request.ReplyChannel, cancellationToken);
        return string.IsNullOrWhiteSpace(recipient) ? null : new InboundReplyTarget(Recipient: recipient, Subject: null, InReplyTo: null, References: null);
    }

    public Task<InboundReplyResult> SendAsync(
        ClarificationRequest request, InboundReplyTarget target, string text, CancellationToken cancellationToken = default)
    {
        return _replyChannel.SendAsync(request.ReplyChannel, target.Recipient, text, cancellationToken);
    }
}
