// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Read-only lookups the employee import needs in bulk: the contracts and groups a row can name, and the
/// existing clients that could be duplicates of the imported rows (same last name, same e-mail, or a
/// customer with the same company).
/// </summary>

using Klacks.Api.Application.Services.ClientImport;

namespace Klacks.Api.Application.Interfaces.ClientImport;

public interface IClientImportLookupRepository
{
    Task<List<ClientImportNamedEntity>> GetContractsAsync(CancellationToken cancellationToken);

    Task<List<ClientImportNamedEntity>> GetGroupsAsync(CancellationToken cancellationToken);

    /// <param name="lowerCaseLastNames">Last names of the import, already lower-cased</param>
    /// <param name="lowerCaseEmails">E-mail addresses of the import, already lower-cased</param>
    /// <param name="lowerCaseCompanies">Companies of the legal-entity rows of the import, trimmed and lower-cased</param>
    Task<List<ClientImportExistingClient>> FindDuplicateCandidatesAsync(
        IReadOnlyCollection<string> lowerCaseLastNames,
        IReadOnlyCollection<string> lowerCaseEmails,
        IReadOnlyCollection<string> lowerCaseCompanies,
        CancellationToken cancellationToken);
}
