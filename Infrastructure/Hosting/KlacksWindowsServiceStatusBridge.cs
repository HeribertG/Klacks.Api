// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Hands the host's application lifetime to the already running Windows service dispatcher, so the service reports
/// "running" once the host has started and can stop the host on a stop request.
/// </summary>
/// <param name="service">Service dispatcher started before the host was built</param>
/// <param name="lifetime">Lifetime of the web host</param>

using System.Runtime.Versioning;

namespace Klacks.Api.Infrastructure.Hosting;

[SupportedOSPlatform("windows")]
public sealed class KlacksWindowsServiceStatusBridge(KlacksWindowsService service, IHostApplicationLifetime lifetime)
    : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        service.AttachApplication(lifetime);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
