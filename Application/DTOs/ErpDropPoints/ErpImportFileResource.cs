// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.ErpDropPoints;

public class ErpImportFileResource
{
    public Guid Id { get; set; }

    public string FileName { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public ErpImportFileStatus Status { get; set; }

    public ErpImportChannel Channel { get; set; }

    public string? UploadedBy { get; set; }

    public DateTime ReceivedAt { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    public int ProcessedOrders { get; set; }

    public int? TotalOrders { get; set; }

    public int NewCount { get; set; }

    public int UpdatedCount { get; set; }

    public int UnchangedCount { get; set; }

    public int SupersededCount { get; set; }

    public int RejectedCount { get; set; }

    public string? FailureReason { get; set; }

    public DateTime? DuplicateOfReceivedAt { get; set; }

    public List<ErpImportRejectedOrderResource> RejectedOrders { get; set; } = [];
}
