// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Interfaces;

public interface IWebSearchProviderFactory
{
    Task<IWebSearchProvider?> CreateAsync(CancellationToken ct = default);
}
