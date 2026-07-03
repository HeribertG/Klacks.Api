// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Imports;

namespace Klacks.Api.Domain.Interfaces.Imports;

public interface IErpDefaultDropPointProvider
{
    Task<ErpDropPoint> GetOrCreateDefaultAsync(CancellationToken cancellationToken = default);
}
