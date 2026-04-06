// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.ComponentModel.DataAnnotations;

namespace Klacks.Api.Domain.Models.Schedules;

/// <summary>
/// Represents an exclusive edit lock on a container resource (template or container work entry).
/// Stale locks (without recent heartbeat) are released automatically on next acquire attempt.
/// </summary>
public class ContainerLock
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(32)]
    public string ResourceType { get; set; } = string.Empty;

    [Required]
    public Guid ResourceId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(256)]
    public string UserName { get; set; } = string.Empty;

    public DateTime AcquiredAt { get; set; }

    public DateTime LastHeartbeatAt { get; set; }
}
