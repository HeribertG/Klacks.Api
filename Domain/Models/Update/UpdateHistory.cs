// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Audit log and hand-off queue row shared between the in-process API (writes execution requests,
/// reads status) and the out-of-process updater (claims pending rows, executes, writes status back).
/// Active rows (Pending/Running) are guarded by a partial unique index so only one operation runs at a time.
/// </summary>
using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Update;

public class UpdateHistory : BaseEntity
{
    public UpdateOperationType OperationType { get; set; }

    public UpdateOperationStatus Status { get; set; }

    public UpdateChannel Channel { get; set; }

    public string FromVersion { get; set; } = string.Empty;

    public string TargetVersion { get; set; } = string.Empty;

    public string? ArtifactRef { get; set; }

    public string? ArtifactSha256 { get; set; }

    public string? ArtifactSignature { get; set; }

    public bool ContainsMigrations { get; set; }

    public string? BackupRef { get; set; }

    public Guid? RelatedOperationId { get; set; }

    public string RequestedBy { get; set; } = string.Empty;

    public DateTime RequestedAt { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? LastHeartbeatAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? Message { get; set; }
}
