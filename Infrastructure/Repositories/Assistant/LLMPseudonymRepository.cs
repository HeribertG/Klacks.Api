// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// EF-backed implementation of ILLMPseudonymRepository: reads clients, users, phone numbers and address street lines for
/// the pseudonym dictionary and allocates the stable numbers of people, addresses and ambiguous names in llm_pseudonyms (next free number per kind, unique per kind and key
/// and per kind and number).
/// </summary>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Pseudonymization;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class LLMPseudonymRepository : ILLMPseudonymRepository
{
    private const string SignatureSql =
        "SELECT concat_ws('|', " +
        "(SELECT count(*) FROM client), (SELECT max(coalesce(update_time, create_time)) FROM client), " +
        "(SELECT count(*) FROM communication), (SELECT max(coalesce(update_time, create_time)) FROM communication), " +
        "(SELECT count(*) FROM address), (SELECT max(coalesce(update_time, create_time)) FROM address), " +
        "(SELECT count(*) FROM \"AspNetUsers\"), " +
        "(SELECT md5(string_agg(coalesce(concurrency_stamp, ''), ',' ORDER BY id)) FROM \"AspNetUsers\")) AS \"Value\"";

    private static readonly CommunicationTypeEnum[] MailTypes = [CommunicationTypeEnum.PrivateMail, CommunicationTypeEnum.OfficeMail];

    private readonly DataBaseContext _context;

    public LLMPseudonymRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<string> GetPersonDataSignatureAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _context.Database.SqlQueryRaw<string>(SignatureSql).ToListAsync(cancellationToken);
        return rows.FirstOrDefault() ?? string.Empty;
    }

    public async Task<IReadOnlyList<PseudonymPerson>> LoadPersonsAsync(CancellationToken cancellationToken = default)
    {
        var clients = await _context.Client
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Select(c => new
            {
                c.Id, c.Type, c.Gender, c.FirstName, c.Name, c.SecondName, c.MaidenName, c.Company, c.LegalEntity, c.IdNumber
            })
            .ToListAsync(cancellationToken);

        var users = await _context.AppUser
            .AsNoTracking()
            .Select(u => new { u.Id, u.FirstName, u.LastName })
            .ToListAsync(cancellationToken);

        var persons = new List<PseudonymPerson>(clients.Count + users.Count);
        persons.AddRange(clients.Select(c => new PseudonymPerson(
            c.Type == EntityTypeEnum.Customer ? PseudonymKind.Customer : PseudonymKind.Employee,
            c.Id.ToString(),
            c.Gender,
            c.FirstName,
            c.Name,
            c.SecondName,
            c.MaidenName,
            c.Company,
            c.LegalEntity,
            c.IdNumber)));
        persons.AddRange(users.Select(u => new PseudonymPerson(
            PseudonymKind.User, u.Id, null, u.FirstName, u.LastName, null, null, null, false, null)));
        return persons;
    }

    public async Task<IReadOnlyList<string>> LoadPhoneNumbersAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Communication
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(c => !MailTypes.Contains(c.Type) && c.Value != string.Empty)
            .Select(c => c.Value)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PseudonymAddress>> LoadAddressesAsync(CancellationToken cancellationToken = default)
    {
        var addresses = await _context.Address
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(a => a.Street != string.Empty || a.Street2 != string.Empty || a.Street3 != string.Empty
                || a.AddressLine1 != string.Empty || a.AddressLine2 != string.Empty)
            .Select(a => new { a.Id, a.Street, a.Street2, a.Street3, a.AddressLine1, a.AddressLine2 })
            .ToListAsync(cancellationToken);

        return addresses
            .Select(a => new PseudonymAddress(a.Id.ToString(), a.Street, a.Street2, a.Street3, a.AddressLine1, a.AddressLine2))
            .ToList();
    }

    public async Task<IReadOnlyDictionary<PseudonymKey, int>> EnsureOrdinalsAsync(
        IReadOnlyCollection<PseudonymKey> keys, CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<PseudonymKey, int>(keys.Count);
        var added = false;
        foreach (var group in keys.Distinct().GroupBy(key => key.Kind))
        {
            var kind = group.Key;
            var wanted = group.Select(key => key.Key).ToList();
            var existing = await _context.LLMPseudonyms
                .AsNoTracking()
                .Where(p => p.Kind == kind && wanted.Contains(p.Key))
                .Select(p => new { p.Key, p.Ordinal })
                .ToListAsync(cancellationToken);
            foreach (var row in existing)
            {
                result[new PseudonymKey(kind, row.Key)] = row.Ordinal;
            }

            var missing = wanted
                .Where(key => !result.ContainsKey(new PseudonymKey(kind, key)))
                .Order(StringComparer.Ordinal)
                .ToList();
            if (missing.Count == 0)
            {
                continue;
            }

            var next = (await _context.LLMPseudonyms
                .Where(p => p.Kind == kind)
                .MaxAsync(p => (int?)p.Ordinal, cancellationToken) ?? 0) + 1;
            foreach (var key in missing)
            {
                var row = new LLMPseudonym { Id = Guid.NewGuid(), Kind = kind, Key = key, Ordinal = next++, CreateTime = DateTime.UtcNow };
                await _context.LLMPseudonyms.AddAsync(row, cancellationToken);
                result[new PseudonymKey(kind, key)] = row.Ordinal;
                added = true;
            }
        }

        if (added)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return result;
    }
}
