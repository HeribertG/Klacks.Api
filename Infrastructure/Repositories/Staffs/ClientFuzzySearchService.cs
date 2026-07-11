// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Fuzzy client-name search over PostgreSQL: combines pg_trgm trigram similarity on the
/// concatenated name fields (typos, partial names) with Kölner Phonetik token overlap on the
/// persisted phonetic_tokens column (misheard spoken names like "Meier" vs "Mayer"). The raw
/// SQL keeps the is_deleted filter and the lower(...) search-text expression in sync with the
/// GIN indexes ix_client_search_text_trgm / ix_client_phonetic_tokens from the
/// AddClientPhoneticTokensAndTrigram migration. Callers apply their own permission/group
/// filters on the returned ids — this service only ranks by name similarity.
/// </summary>
/// <param name="context">Database context the raw SQL runs against</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Clients;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Staffs;

public class ClientFuzzySearchService : IClientFuzzySearchService
{
    // Phonetic code equality is a stronger "same name" signal than a grey-zone trigram score,
    // so a phonetic hit outranks trigram-only hits regardless of their similarity value.
    private const double PhoneticBoost = 1.0;

    private readonly DataBaseContext _context;

    public ClientFuzzySearchService(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<List<Client>> SearchAsync(string query, int limit, CancellationToken cancellationToken = default)
    {
        var trimmed = (query ?? string.Empty).Trim();
        if (trimmed.Length == 0 || limit <= 0)
        {
            return [];
        }

        var lowered = trimmed.ToLowerInvariant();
        var phoneticCodes = ClientPhoneticTokenBuilder.Build(trimmed) ?? string.Empty;

        return await _context.Client
            .FromSqlInterpolated($@"
SELECT c.*
FROM client c
WHERE c.is_deleted = false
  AND (
    lower(coalesce(c.name, '') || ' ' || coalesce(c.first_name, '') || ' ' || coalesce(c.maiden_name, '') || ' ' || coalesce(c.company, '')) % {lowered}
    OR (c.phonetic_tokens IS NOT NULL AND c.phonetic_tokens <> '' AND string_to_array(c.phonetic_tokens, ' ') && string_to_array({phoneticCodes}, ' '))
  )
ORDER BY
  similarity(lower(coalesce(c.name, '') || ' ' || coalesce(c.first_name, '') || ' ' || coalesce(c.maiden_name, '') || ' ' || coalesce(c.company, '')), {lowered})
  + CASE WHEN c.phonetic_tokens IS NOT NULL AND c.phonetic_tokens <> '' AND string_to_array(c.phonetic_tokens, ' ') && string_to_array({phoneticCodes}, ' ')
         THEN {PhoneticBoost} ELSE 0 END DESC
LIMIT {limit}")
            .IgnoreQueryFilters()
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
