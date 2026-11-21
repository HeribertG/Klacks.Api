// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Returns the full licence text of the local voice model (BigScience OpenRAIL-M of Supertonic 3, shipped as an
/// embedded resource of Klacks.Api) together with the version id the administrator consents to.
/// </summary>
/// <param name="licenseText">Supplies the embedded licence text</param>
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class GetLocalTtsLicenseQueryHandler : IRequestHandler<GetLocalTtsLicenseQuery, LocalTtsLicense>
{
    private readonly ILocalTtsLicenseText _licenseText;

    public GetLocalTtsLicenseQueryHandler(ILocalTtsLicenseText licenseText)
    {
        _licenseText = licenseText;
    }

    public Task<LocalTtsLicense> Handle(GetLocalTtsLicenseQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(new LocalTtsLicense(LocalTtsConstants.LicenseVersion, _licenseText.Text));
}
