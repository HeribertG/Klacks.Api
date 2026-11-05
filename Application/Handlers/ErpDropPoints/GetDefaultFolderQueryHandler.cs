// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler that tells the Import settings card where the ERP writes its files: on a native installation the absolute
/// folder, on Docker the host folder when the compose file reports it (with the container path alongside), otherwise
/// the container path. The installation kind comes from the same ErpHandoverAdvisor the assistant skills use, so the
/// card and Klacksy give the same answer.
/// </summary>
using Klacks.Api.Application.DTOs.ErpDropPoints;
using Klacks.Api.Application.Queries.ErpDropPoints;
using Klacks.Api.Application.Services.Imports;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Imports;
using Klacks.Api.Domain.Models.Imports;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ErpDropPoints;

public class GetDefaultFolderQueryHandler : IRequestHandler<GetDefaultFolderQuery, ErpDropFolderResource>
{
    private readonly IErpDefaultDropPointProvider _defaultDropPointProvider;
    private readonly ErpDropFolderResolver _folderResolver;

    public GetDefaultFolderQueryHandler(IErpDefaultDropPointProvider defaultDropPointProvider, ErpDropFolderResolver folderResolver)
    {
        _defaultDropPointProvider = defaultDropPointProvider;
        _folderResolver = folderResolver;
    }

    public async Task<ErpDropFolderResource> Handle(GetDefaultFolderQuery request, CancellationToken cancellationToken)
    {
        var dropPoint = await _defaultDropPointProvider.GetOrCreateDefaultAsync(cancellationToken);
        return ToResource(_folderResolver.Resolve(dropPoint.BucketPrefix));
    }

    private static ErpDropFolderResource ToResource(ErpDropFolder folder)
    {
        var isDocker = folder.Guidance.Installation == ErpHandoverInstallations.Docker;
        var isHostPath = folder.HostFolderPath != null;

        return new ErpDropFolderResource
        {
            Installation = folder.Guidance.Installation,
            FolderPath = folder.HostFolderPath ?? folder.FolderPath,
            ContainerPath = isDocker ? folder.FolderPath : null,
            IsHostPath = isHostPath,
        };
    }
}
