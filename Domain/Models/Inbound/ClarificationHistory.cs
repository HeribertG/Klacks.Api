// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Inbound;

public sealed record ClarificationHistory(
    string OriginalText,
    DateTime OriginalReceivedAt,
    string Question,
    DateTime AskedAt);
