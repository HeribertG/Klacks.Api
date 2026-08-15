// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Configures the SignalR backplane that carries hub messages between API instances. Without it a
/// push only reaches clients connected to the instance that sent it, which is invisible rather than
/// loud: the other clients simply never update. Left empty the API runs as a single instance and
/// keeps the in-process behaviour, so existing installations are unaffected until they opt in.
/// </summary>

namespace Klacks.Api.Application.Configuration;

public class SignalRBackplaneOptions
{
    public const string SectionName = "SignalR:Backplane";

    /// <summary>
    /// StackExchange.Redis connection string. Empty means no backplane and therefore a single instance.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Prefix for the Redis channels, so several Klacks deployments can share one Redis instance
    /// without delivering each other's hub messages.
    /// </summary>
    public string ChannelPrefix { get; set; } = "klacks";
}
