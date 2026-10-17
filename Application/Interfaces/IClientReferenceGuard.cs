// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Interfaces;

/// <summary>
/// Checks a client id a write sets as a reference (for example the customer of a shift): the client must exist, must
/// not be soft-deleted and must be visible to the calling user. A hidden, a missing and a deleted client get the same
/// answer, so the reference cannot be used to probe for clients of foreign groups.
/// </summary>
public interface IClientReferenceGuard
{
    /// <summary>
    /// True only when the client exists (not soft-deleted) and the caller may see it.
    /// </summary>
    /// <param name="clientId">Client id taken from the request</param>
    Task<bool> ExistsVisibleAsync(Guid clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Throws KeyNotFoundException with ClientVisibilityMessages.ClientNotFound unless ExistsVisibleAsync holds.
    /// </summary>
    /// <param name="clientId">Client id taken from the request</param>
    Task EnsureExistsVisibleAsync(Guid clientId, CancellationToken cancellationToken = default);
}
