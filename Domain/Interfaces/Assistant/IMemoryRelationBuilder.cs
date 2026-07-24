// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IMemoryRelationBuilder
{
    Task<int> BuildAsync(CancellationToken cancellationToken = default);
}
