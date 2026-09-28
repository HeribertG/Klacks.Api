// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.ComponentModel.DataAnnotations;
using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Imports;

public class ErpImportException : BaseEntity
{
    [Required]
    [MaxLength(200)]
    public string SourceSystemId { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string FileKey { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? ExternalOrderReference { get; set; }

    [Required]
    [MaxLength(2000)]
    public string Reason { get; set; } = string.Empty;

    public DateTime? ResolvedAt { get; set; }
}
