// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.IdentityProviders;

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
