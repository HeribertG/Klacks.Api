// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
