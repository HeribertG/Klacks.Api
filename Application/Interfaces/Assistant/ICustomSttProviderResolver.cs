// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Interfaces.Assistant;

/// <summary>
/// Resolves an STT provider id of the form custom:{guid} to the enabled self-hosted provider row.
/// </summary>
public interface ICustomSttProviderResolver
{
    bool IsCustomProviderId(string providerId);

    Task<CustomSttProvider?> TryResolveAsync(string providerId, CancellationToken ct = default);
}
