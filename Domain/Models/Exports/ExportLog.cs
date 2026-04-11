// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Exports;

/// <summary>
/// History entry for every successful order export run.
/// Used to warn admins when unsealing a period that has already been exported.
/// </summary>
public class ExportLog : BaseEntity
{
    public string Format { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public Guid? GroupId { get; set; }

    public string Language { get; set; } = "de";

    public string CurrencyCode { get; set; } = "EUR";

    public string FileName { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public int RecordCount { get; set; }

    public DateTime ExportedAt { get; set; }

    public string ExportedBy { get; set; } = string.Empty;
}
