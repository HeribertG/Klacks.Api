// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Inbound;

public sealed record ComposedClarificationQuestion(
    string Question,
    string? ShiftContext,
    DateTime? ShiftStartUtc);
