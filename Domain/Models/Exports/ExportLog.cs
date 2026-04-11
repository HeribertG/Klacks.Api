// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.ComponentModel.DataAnnotations;
using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Exports;

/// <summary>
/// History entry for every successful order export run.
/// Used to warn admins when unsealing a period that has already been exported.
/// </summary>
/// <remarks>
/// ExportedAt and ExportedBy are deliberately kept separate from BaseEntity.CreateTime
/// and CurrentUserCreated: the audit moment is a domain concept that must remain stable
/// even if the row-insert infrastructure later changes its semantics (batching, retries,
/// snapshot restores). Do not remove these fields in favour of the inherited ones.
/// </remarks>
public class ExportLog : BaseEntity
{
    [MaxLength(16)]
    public string Format { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public Guid? GroupId { get; set; }

    [MaxLength(16)]
    public string Language { get; set; } = "de";

    [MaxLength(16)]
    public string CurrencyCode { get; set; } = "EUR";

    [MaxLength(260)]
    public string FileName { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public int RecordCount { get; set; }

    public DateTime ExportedAt { get; set; }

    [MaxLength(256)]
    public string ExportedBy { get; set; } = string.Empty;
}
