// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Bulk read-only lookups of the employee import. The duplicate search runs two set-based queries
/// (lower(name) IN ..., lower(mail) IN ...) instead of one query per row; ToLower is used because
/// Npgsql translates it to lower(), which ToLowerInvariant is not. Customers are excluded: an employee
/// is only a duplicate of another employee or external employee.
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
        CancellationToken cancellationToken)
    {
        var names = lowerCaseLastNames.Distinct().ToList();
        var emails = lowerCaseEmails.Distinct().ToList();

        var clientIdsByMail = emails.Count == 0
            ? []
            : await ClientIdsByMailQuery(emails).ToListAsync(cancellationToken);

        if (names.Count == 0 && clientIdsByMail.Count == 0)
        {
            return [];
        }

        return await CandidatesQuery(names, clientIdsByMail).ToListAsync(cancellationToken);
    }

    internal IQueryable<Guid> ClientIdsByMailQuery(List<string> lowerCaseEmails) =>
        _context.Communication.AsNoTracking()
            .Where(c => (c.Type == CommunicationTypeEnum.PrivateMail || c.Type == CommunicationTypeEnum.OfficeMail)
                        && lowerCaseEmails.Contains(c.Value.ToLower()))
            .Select(c => c.ClientId)
            .Distinct();

    internal IQueryable<ClientImportExistingClient> CandidatesQuery(List<string> lowerCaseLastNames, List<Guid> clientIds) =>
        _context.Client.AsNoTracking()
            .Where(c => c.Type != EntityTypeEnum.Customer
                        && (lowerCaseLastNames.Contains(c.Name.ToLower()) || clientIds.Contains(c.Id)))
            .Select(c => new ClientImportExistingClient(
                c.Id,
                c.FirstName,
                c.Name,
                c.Birthdate,
                c.Communications
                    .Where(m => m.Type == CommunicationTypeEnum.PrivateMail || m.Type == CommunicationTypeEnum.OfficeMail)
                    .Select(m => m.Value)
                    .ToList()));
}
