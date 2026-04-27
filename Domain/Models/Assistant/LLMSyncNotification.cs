// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Stores the result of one provider's model sync run for admin notification.
/// </summary>
/// <param name="ProviderId">Identifier of the provider that was synced</param>
/// <param name="NewModelNames">Names of models added during this sync</param>
/// <param name="DeactivatedModelNames">Names of models disabled during this sync</param>
using System.ComponentModel.DataAnnotations;
using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Assistant;

public class LLMSyncNotification : BaseEntity
{
    [Required]
    [MaxLength(50)]
    public string ProviderId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ProviderName { get; set; } = string.Empty;

    public int NewModelsCount { get; set; }

    public int DeactivatedModelsCount { get; set; }

    public List<string> NewModelNames { get; set; } = [];

    public List<string> DeactivatedModelNames { get; set; } = [];

    public DateTime SyncedAt { get; set; }

    public bool IsRead { get; set; }
}
