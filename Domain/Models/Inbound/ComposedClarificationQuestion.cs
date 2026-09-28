// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Inbound;

public sealed record ComposedClarificationQuestion(
    string Question,
    string? ShiftContext,
    DateTime? ShiftStartUtc);
