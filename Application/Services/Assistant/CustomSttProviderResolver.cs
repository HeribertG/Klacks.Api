// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Resolves an STT provider id of the form custom:{guid} to the matching enabled row of the self-hosted
/// provider table; returns null for built-in ids, malformed guids, unknown or disabled providers.
/// </summary>
/// <param name="repository">Reads custom STT provider rows by id</param>

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant;

public class CustomSttProviderResolver : ICustomSttProviderResolver
{
    private readonly ICustomSttProviderRepository _repository;

    public CustomSttProviderResolver(ICustomSttProviderRepository repository)
    {
        _repository = repository;
    }

    public bool IsCustomProviderId(string providerId)
        => providerId.StartsWith(SttProviderConstants.CustomProviderPrefix, StringComparison.OrdinalIgnoreCase);

    public async Task<CustomSttProvider?> TryResolveAsync(string providerId, CancellationToken ct = default)
    {
        if (!IsCustomProviderId(providerId))
            return null;

        if (!Guid.TryParse(providerId[SttProviderConstants.CustomProviderPrefix.Length..], out var id))
            return null;

        var provider = await _repository.GetByIdAsync(id, ct);
        return provider is { IsEnabled: true } ? provider : null;
    }
}
