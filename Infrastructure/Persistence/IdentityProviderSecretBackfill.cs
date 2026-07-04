// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One-time startup step that re-encrypts IdentityProvider.BindPassword/ClientSecret values still
/// stored in plaintext from before at-rest encryption was introduced. Forces EF's change tracker to
/// rewrite those columns so the configured EncryptedStringConverter runs on save.
/// </summary>
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Persistence;

public interface IIdentityProviderSecretBackfill
{
    Task EncryptLegacySecretsAsync();
}

public class IdentityProviderSecretBackfill : IIdentityProviderSecretBackfill
{
    private const string EncryptedPrefix = "ENC:";

    private readonly DataBaseContext _context;
    private readonly ILogger<IdentityProviderSecretBackfill> _logger;

    public IdentityProviderSecretBackfill(DataBaseContext context, ILogger<IdentityProviderSecretBackfill> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task EncryptLegacySecretsAsync()
    {
        var rows = await _context.Database
            .SqlQuery<LegacySecretRow>(
                $"SELECT id, bind_password, client_secret FROM identity_providers WHERE is_deleted = false")
            .ToListAsync();

        var legacyIds = rows
            .Where(r => IsLegacyPlaintext(r.BindPassword) || IsLegacyPlaintext(r.ClientSecret))
            .Select(r => r.Id)
            .ToList();

        if (legacyIds.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Re-encrypting {Count} legacy identity provider secret(s)", legacyIds.Count);

        foreach (var id in legacyIds)
        {
            var entity = await _context.IdentityProviders.FirstAsync(p => p.Id == id);
            var entry = _context.Entry(entity);

            entry.Property(p => p.BindPassword).IsModified = true;
            entry.Property(p => p.ClientSecret).IsModified = true;
        }

        await _context.SaveChangesAsync();
    }

    private static bool IsLegacyPlaintext(string? value)
    {
        return !string.IsNullOrEmpty(value) && !value.StartsWith(EncryptedPrefix, StringComparison.Ordinal);
    }

    private class LegacySecretRow
    {
        public Guid Id { get; set; }

        public string? BindPassword { get; set; }

        public string? ClientSecret { get; set; }
    }
}
