// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.PeriodClosing;

public record PeriodScheduleNoteIssueRow(
    DateOnly Date,
    Guid ClientId,
    string? ClientFirstName,
    string ClientName,
    string Content);
