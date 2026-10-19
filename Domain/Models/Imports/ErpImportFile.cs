// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.ComponentModel.DataAnnotations;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Imports;

public class ErpImportFile : BaseEntity
{
    public Guid DropPointId { get; set; }

    public virtual ErpDropPoint DropPoint { get; set; } = null!;

    [Required]
    [MaxLength(ErpImportFileLimits.SourceSystemIdMaxLength)]
    public string SourceSystemId { get; set; } = string.Empty;

    [Required]
    [MaxLength(ErpImportFileLimits.StorageKeyMaxLength)]
    public string StorageKey { get; set; } = string.Empty;

    [Required]
    [MaxLength(ErpImportFileLimits.FileNameMaxLength)]
    public string FileName { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    [Required]
    [MaxLength(ErpImportFileLimits.Sha256HexLength)]
    public string Sha256 { get; set; } = string.Empty;

    public ErpImportChannel Channel { get; set; }

    [MaxLength(ErpImportFileLimits.UploadedByMaxLength)]
    public string? UploadedBy { get; set; }

    public ErpImportFileStatus Status { get; set; }

    public DateTime ReceivedAt { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? HeartbeatAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    public int Attempt { get; set; }

    [MaxLength(ErpImportFileLimits.ClaimedByMaxLength)]
    public string? ClaimedBy { get; set; }

    public int? TotalOrders { get; set; }

    public int ProcessedOrders { get; set; }

    public int NewCount { get; set; }

    public int UpdatedCount { get; set; }

    public int UnchangedCount { get; set; }

    public int SupersededCount { get; set; }

    public int RejectedCount { get; set; }

    [MaxLength(ErpImportFileLimits.FailureReasonMaxLength)]
    public string? FailureReason { get; set; }

    public Guid? DuplicateOfFileId { get; set; }
}
