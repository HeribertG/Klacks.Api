// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IAnswerGroundingNameResolver
{
    Task<IReadOnlyList<string>> ResolveClientNamesAsync(
        IReadOnlyList<string> candidates,
        CancellationToken cancellationToken = default);
}
