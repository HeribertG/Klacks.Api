// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Resolves a drop point's folder the same way for the Import settings card and the assistant skills: the absolute
/// path through the object storage resolver the import runner uses, the host-side path from the compose-reported
/// host path hint (Docker only) and the hand-over advice that fits the installation.
/// </summary>
/// <param name="objectStorageService">Resolves a storage key to the absolute folder the Klacks process sees</param>
/// <param name="installationIdentityProvider">Tells a native installation from the Docker stack</param>
/// <param name="options">ErpObjectStorage settings; only HostPathHint is read here</param>

using Klacks.Api.Domain.Interfaces.Imports;
using Klacks.Api.Domain.Interfaces.Update;
using Klacks.Api.Domain.Models.Imports;
using Klacks.Api.Domain.Models.Update;
using Klacks.Api.Domain.Services.Imports;
using Microsoft.Extensions.Options;

namespace Klacks.Api.Application.Services.Imports;

public class ErpDropFolderResolver
{
    private readonly IObjectStorageService _objectStorageService;
    private readonly IInstallationIdentityProvider _installationIdentityProvider;
    private readonly IOptions<ErpObjectStorageOptions> _options;

    public ErpDropFolderResolver(
        IObjectStorageService objectStorageService,
        IInstallationIdentityProvider installationIdentityProvider,
        IOptions<ErpObjectStorageOptions> options)
    {
        _objectStorageService = objectStorageService;
        _installationIdentityProvider = installationIdentityProvider;
        _options = options;
    }

    public ErpDropFolder Resolve(string bucketPrefix)
    {
        var normalizedPrefix = ErpImportStorageKeys.NormalizePrefix(bucketPrefix);
        var folderPath = ErpFolderDisplayPath.WithoutTrailingSeparator(_objectStorageService.ResolvePath(normalizedPrefix));
        var identity = _installationIdentityProvider.Get();
        var hostFolderPath = identity.Kind == InstallationKind.Docker
            ? ErpHostFolderPath.Combine(_options.Value.HostPathHint, normalizedPrefix)
            : null;

        return new ErpDropFolder(folderPath, hostFolderPath, ErpHandoverAdvisor.Advise(identity, folderPath, hostFolderPath));
    }
}
