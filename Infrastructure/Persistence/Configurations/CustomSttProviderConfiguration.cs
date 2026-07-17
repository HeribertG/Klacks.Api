// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the CustomSttProvider entity with query filter and
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

public class CustomSttProviderConfiguration : IEntityTypeConfiguration<CustomSttProvider>
{
    private const int ApiKeyMaxLength = 2000;

    private readonly ISettingsEncryptionService? _encryptionService;

    public CustomSttProviderConfiguration(ISettingsEncryptionService? encryptionService)
    {
        _encryptionService = encryptionService;
    }

    public void Configure(EntityTypeBuilder<CustomSttProvider> builder)
    {
        builder.ToTable("custom_stt_providers");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.ConnectionType).HasMaxLength(20).IsRequired();
        builder.Property(e => e.ApiUrl).HasMaxLength(500).IsRequired();
        builder.Property(e => e.ApiKey).HasMaxLength(ApiKeyMaxLength);
        builder.Property(e => e.LanguageModel).HasMaxLength(200);
        builder.HasQueryFilter(e => !e.IsDeleted);

        if (_encryptionService != null)
        {
            builder.Property(e => e.ApiKey).HasConversion(new EncryptedStringConverter(_encryptionService));
        }
    }
}
