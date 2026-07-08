// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the LLMProvider-Entity with query filter, JSONB settings and
/// at-rest encryption of ApiKey.
/// </summary>
/// <param name="encryptionService">
/// Encrypts/decrypts ApiKey transparently at the DB boundary. Null when the context is
/// constructed directly outside DI (e.g. tests) — in that case the key is stored as-is.
/// </param>
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class LLMProviderConfiguration : IEntityTypeConfiguration<LLMProvider>
{
    private readonly ISettingsEncryptionService? _encryptionService;

    public LLMProviderConfiguration(ISettingsEncryptionService? encryptionService)
    {
        _encryptionService = encryptionService;
    }

    public void Configure(EntityTypeBuilder<LLMProvider> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.Property(e => e.Settings).HasJsonbConversionWithComparer<Dictionary<string, object>>();

        if (_encryptionService != null)
        {
            builder.Property(e => e.ApiKey).HasConversion(new EncryptedStringConverter(_encryptionService));
        }
    }
}
