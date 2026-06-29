// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.PeriodClosing;

public record PeriodScheduleNoteIssueRow(
    DateOnly Date,
    Guid ClientId,
    string? ClientFirstName,
    string ClientName,
    string Content);
