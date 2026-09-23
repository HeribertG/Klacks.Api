// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Inbound;

public sealed record InboundReplyTarget(
    string Recipient,
    string? Subject,
    string? InReplyTo,
    string? References);
