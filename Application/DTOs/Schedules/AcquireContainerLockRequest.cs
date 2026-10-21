// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

public class AcquireContainerLockRequest
{
    public string ResourceType { get; set; } = string.Empty;

    public Guid ResourceId { get; set; }

    public string InstanceId { get; set; } = string.Empty;
}
