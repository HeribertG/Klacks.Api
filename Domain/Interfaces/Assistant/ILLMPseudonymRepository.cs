// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Services.Assistant.Pseudonymization;

namespace Klacks.Api.Domain.Interfaces.Assistant;

/// <summary>
/// Reads the person data the pseudonym dictionary is built from and allocates the stable pseudonym numbers of the
/// installation for people, addresses and ambiguous names (keys are built from entity ids, never from names or values).
/// <see cref="EnsureOrdinalsAsync"/> commits by itself (it is called from a cache outside any request unit of work); a
/// unique-index conflict with another API process fails that request closed, the next call reads the winner's row.
/// </summary>
public interface ILLMPseudonymRepository
{
    /// <summary>
    /// A cheap fingerprint of clients, communications, addresses and users; it changes whenever one of them is added, changed or
    /// removed, so the cached dictionary knows when to rebuild.
    /// </summary>
    Task<string> GetPersonDataSignatureAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// All clients (every type, soft-deleted ones included, because they can appear in history) and all users.
    /// </summary>
    Task<IReadOnlyList<PseudonymPerson>> LoadPersonsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> LoadPhoneNumbersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The street lines of every client address with at least one non-empty line (soft-deleted ones included);
    /// postcode, town, state and country are not read.
    /// </summary>
    Task<IReadOnlyList<PseudonymAddress>> LoadAddressesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the number of every requested key, allocating the next free number per kind for keys that have none
    /// yet, and commits the new rows. Callers serialise calls within the process.
    /// </summary>
    Task<IReadOnlyDictionary<PseudonymKey, int>> EnsureOrdinalsAsync(
        IReadOnlyCollection<PseudonymKey> keys, CancellationToken cancellationToken = default);
}
