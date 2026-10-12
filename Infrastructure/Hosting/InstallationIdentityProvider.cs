// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Derives the installation identity from the hosting configuration: a configured Hosting:DataRoot marks a native
/// installation, everything else is the Docker stack. The runtime identifier is the one of the running process.
/// </summary>
/// <param name="configuration">Effective configuration; only Hosting:DataRoot is read</param>

using System.Runtime.InteropServices;
using Klacks.Api.Domain.Interfaces.Update;
using Klacks.Api.Domain.Models.Update;

namespace Klacks.Api.Infrastructure.Hosting;

public class InstallationIdentityProvider : IInstallationIdentityProvider
{
    private readonly InstallationIdentity _identity;

    public InstallationIdentityProvider(IConfiguration configuration)
    {
        var kind = DataRootPaths.GetDataRoot(configuration) is null ? InstallationKind.Docker : InstallationKind.Native;
        _identity = new InstallationIdentity(kind, RuntimeInformation.RuntimeIdentifier);
    }

    public InstallationIdentity Get() => _identity;
}
