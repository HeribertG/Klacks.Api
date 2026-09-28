// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Inbound;

public sealed record InboundReplyTarget(
    string Recipient,
    string? Subject,
    string? InReplyTo,
    string? References);
