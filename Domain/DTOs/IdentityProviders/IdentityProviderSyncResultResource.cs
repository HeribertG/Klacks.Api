// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.DTOs.IdentityProviders;

public class IdentityProviderSyncResultResource
{
    public bool Success { get; set; }

    public int TotalProcessed { get; set; }

    public int NewClients { get; set; }

    public int UpdatedClients { get; set; }

    public int DeactivatedClients { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime SyncTime { get; set; }
}
