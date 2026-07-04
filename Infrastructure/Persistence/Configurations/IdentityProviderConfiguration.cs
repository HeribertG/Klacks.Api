// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the IdentityProvider-Entity with query filter, JSONB attribute mapping,
/// at-rest encryption of BindPassword/ClientSecret and Index.
/// </summary>
/// <param name="encryptionService">
/// Encrypts/decrypts BindPassword and ClientSecret transparently at the DB boundary. Null when the
/// context is constructed directly outside DI (e.g. tests) — in that case secrets are stored as-is.
/// </param>
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class IdentityProviderConfiguration : IEntityTypeConfiguration<IdentityProvider>
{
    private readonly ISettingsEncryptionService? _encryptionService;

    public IdentityProviderConfiguration(ISettingsEncryptionService? encryptionService)
    {
        _encryptionService = encryptionService;
    }

    public void Configure(EntityTypeBuilder<IdentityProvider> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.Property(e => e.AttributeMapping).HasJsonbConversionWithComparer<Dictionary<string, string>>();
        builder.HasIndex(p => new { p.IsDeleted, p.IsEnabled, p.SortOrder });

        if (_encryptionService != null)
        {
            var secretConverter = new EncryptedStringConverter(_encryptionService);
            builder.Property(e => e.BindPassword).HasConversion(secretConverter);
            builder.Property(e => e.ClientSecret).HasConversion(secretConverter);
        }
    }
}
