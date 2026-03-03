// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Schedules;

public class BreakPlaceholderResource
{
    public Guid AbsenceId { get; set; }

    public Guid ClientId { get; set; }

    public EntrySource EntrySource { get; set; } = EntrySource.Placeholder;

    public DateTime From { get; set; }

    public Guid Id { get; set; }

    public string? Information { get; set; }

    public DateTime Until { get; set; }
}
