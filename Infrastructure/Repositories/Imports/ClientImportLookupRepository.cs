// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Bulk read-only lookups of the employee import. The duplicate search runs two set-based queries
/// (lower(name) IN ..., lower(mail) IN ..., lower(trim(company)) IN ... for customers) instead of one query
/// per row; ToLower is used because Npgsql translates it to lower(), which ToLowerInvariant is not. All
/// client types are returned with their type, because ClientImportDuplicateKeys compares customers only
/// with customers and employees only with employees or external employees; the addresses (postcode,
/// street) are loaded for legal-entity customers only, for the company + postcode + street business key.
/// </summary>
/// <param name="context">Database context</param>

using Klacks.Api.Application.Interfaces.ClientImport;
using Klacks.Api.Application.Services.ClientImport;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Imports;

public class ClientImportLookupRepository : IClientImportLookupRepository
{
    private readonly DataBaseContext _context;

    public ClientImportLookupRepository(DataBaseContext context)
    {
        _context = context;
    }

    public Task<List<ClientImportNamedEntity>> GetContractsAsync(CancellationToken cancellationToken) =>
        _context.Contract.AsNoTracking()
            .Select(c => new ClientImportNamedEntity(c.Id, c.Name))
            .ToListAsync(cancellationToken);

    public Task<List<ClientImportNamedEntity>> GetGroupsAsync(CancellationToken cancellationToken) =>
        _context.Group.AsNoTracking()
            .Select(g => new ClientImportNamedEntity(g.Id, g.Name))
            .ToListAsync(cancellationToken);

    public async Task<List<ClientImportExistingClient>> FindDuplicateCandidatesAsync(
        IReadOnlyCollection<string> lowerCaseLastNames,
        IReadOnlyCollection<string> lowerCaseEmails,
        IReadOnlyCollection<string> lowerCaseCompanies,
        CancellationToken cancellationToken)
    {
        var names = lowerCaseLastNames.Distinct().ToList();
        var emails = lowerCaseEmails.Distinct().ToList();
        var companies = lowerCaseCompanies.Distinct().ToList();

        var clientIdsByMail = emails.Count == 0
            ? []
            : await ClientIdsByMailQuery(emails).ToListAsync(cancellationToken);

        if (names.Count == 0 && clientIdsByMail.Count == 0 && companies.Count == 0)
        {
            return [];
        }

        return await CandidatesQuery(names, clientIdsByMail, companies).ToListAsync(cancellationToken);
    }

    internal IQueryable<Guid> ClientIdsByMailQuery(List<string> lowerCaseEmails) =>
        _context.Communication.AsNoTracking()
            .Where(c => (c.Type == CommunicationTypeEnum.PrivateMail || c.Type == CommunicationTypeEnum.OfficeMail)
                        && lowerCaseEmails.Contains(c.Value.ToLower()))
            .Select(c => c.ClientId)
            .Distinct();

    internal IQueryable<ClientImportExistingClient> CandidatesQuery(
        List<string> lowerCaseLastNames, List<Guid> clientIds, List<string> lowerCaseCompanies) =>
        _context.Client.AsNoTracking()
            .Where(c => lowerCaseLastNames.Contains(c.Name.ToLower())
                        || clientIds.Contains(c.Id)
                        || (c.Type == EntityTypeEnum.Customer && c.Company != null && lowerCaseCompanies.Contains(c.Company.Trim().ToLower())))
            .Select(c => new ClientImportExistingClient(
                c.Id,
                c.FirstName,
                c.Name,
                c.Birthdate,
                c.Communications
                    .Where(m => m.Type == CommunicationTypeEnum.PrivateMail || m.Type == CommunicationTypeEnum.OfficeMail)
                    .Select(m => m.Value)
                    .ToList(),
                c.Type,
                c.LegalEntity,
                c.Company,
                c.Addresses
                    .Where(a => c.Type == EntityTypeEnum.Customer && c.LegalEntity && !a.IsDeleted)
                    .Select(a => new ClientImportExistingAddress(a.Zip, a.Street))
                    .ToList()));
}
