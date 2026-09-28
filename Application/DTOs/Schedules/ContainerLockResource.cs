// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

public class ContainerLockResource
{
    public Guid Id { get; set; }

    public string ResourceType { get; set; } = string.Empty;

    public Guid ResourceId { get; set; }

    public Guid UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string InstanceId { get; set; } = string.Empty;

    public DateTime AcquiredAt { get; set; }

    public DateTime LastHeartbeatAt { get; set; }

    public bool Acquired { get; set; }

    public bool IsSelfConflict { get; set; }
}

public class AcquireContainerLockRequest
{
    public string ResourceType { get; set; } = string.Empty;

    public Guid ResourceId { get; set; }

    public string InstanceId { get; set; } = string.Empty;
}
