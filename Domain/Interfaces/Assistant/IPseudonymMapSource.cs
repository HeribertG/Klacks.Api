// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Services.Assistant.Pseudonymization;

namespace Klacks.Api.Domain.Interfaces.Assistant;

/// <summary>
/// Hands out the pseudonym dictionary of the installation, rebuilt whenever clients, communications or users change.
/// Throws when the data
/// cannot be read; callers must then refuse to send rather than send plain text.
/// </summary>
public interface IPseudonymMapSource
{
    Task<PseudonymDictionary> GetDictionaryAsync(CancellationToken cancellationToken = default);
}
