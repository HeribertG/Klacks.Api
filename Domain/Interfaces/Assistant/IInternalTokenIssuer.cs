// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IInternalTokenIssuer
{
    /// <param name="ownerUserId">The account the background work runs under</param>
    /// <param name="capToRole">Optional ceiling: the token is issued with at most this role. Never
    /// raises a caller above their real roles.</param>
    Task<InternalTokenResult> IssueForOwnerAsync(
        Guid ownerUserId,
        string? capToRole = null,
        CancellationToken cancellationToken = default);
}
