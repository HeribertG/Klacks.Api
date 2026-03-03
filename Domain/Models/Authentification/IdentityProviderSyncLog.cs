// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Domain.Models.Authentification;

public class IdentityProviderSyncLog : BaseEntity
{
    [Required]
    public Guid IdentityProviderId { get; set; }

    public virtual IdentityProvider IdentityProvider { get; set; } = null!;

    [Required]
    public Guid ClientId { get; set; }

    public virtual Client Client { get; set; } = null!;

    [Required]
    [MaxLength(500)]
    public string ExternalId { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ExternalDn { get; set; }

    public DateTime LastSyncTime { get; set; }

    public bool IsActiveInSource { get; set; }

    [MaxLength(500)]
    public string? SyncError { get; set; }
}
