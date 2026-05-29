// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Update;

namespace Klacks.Api.Domain.Interfaces.Update;

public interface IUpdateManifestReader
{
    Task<UpdateManifest?> GetManifestAsync(UpdateChannel channel, CancellationToken cancellationToken = default);
}
