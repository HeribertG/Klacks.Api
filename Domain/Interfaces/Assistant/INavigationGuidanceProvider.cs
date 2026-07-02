// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface INavigationGuidanceProvider
{
    bool CanHandle(string pageKey);

    Task<string?> GetGuidanceAsync(string pageKey, Guid entityId, CancellationToken cancellationToken = default);
}
