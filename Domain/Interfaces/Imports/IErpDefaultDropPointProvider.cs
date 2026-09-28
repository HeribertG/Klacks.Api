// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Imports;

namespace Klacks.Api.Domain.Interfaces.Imports;

public interface IErpDefaultDropPointProvider
{
    Task<ErpDropPoint> GetOrCreateDefaultAsync(CancellationToken cancellationToken = default);
}
