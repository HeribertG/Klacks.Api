// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.PeriodClosing;

/// <summary>
/// History entry for a completed order export run including scope, file metadata, and operator.
/// </summary>
public class ExportLogDto
{
    public Guid Id { get; set; }

    public string Format { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public Guid? GroupId { get; set; }

    public string? GroupName { get; set; }

    public string Language { get; set; } = "de";

    public string CurrencyCode { get; set; } = "EUR";

    public string FileName { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public int RecordCount { get; set; }

    public DateTime ExportedAt { get; set; }

    public string ExportedBy { get; set; } = string.Empty;

    public string? ExportedByName { get; set; }
}
