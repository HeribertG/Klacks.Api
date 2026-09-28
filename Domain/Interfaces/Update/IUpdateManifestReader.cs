// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Update;

namespace Klacks.Api.Domain.Interfaces.Update;

public interface IUpdateManifestReader
{
    Task<UpdateManifest?> GetManifestAsync(UpdateChannel channel, CancellationToken cancellationToken = default);
}
