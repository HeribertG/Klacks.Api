// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IAnswerGroundingNameResolver
{
    Task<IReadOnlyList<string>> ResolveClientNamesAsync(
        IReadOnlyList<string> candidates,
        CancellationToken cancellationToken = default);
}
