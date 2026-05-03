// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the SentimentKeywordSet-Entity with table name, JSONB-Keywords and unique index.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class SentimentKeywordSetConfiguration : IEntityTypeConfiguration<SentimentKeywordSet>
{
    public void Configure(EntityTypeBuilder<SentimentKeywordSet> builder)
    {
        builder.ToTable("sentiment_keyword_sets");
        builder.Property(e => (Dictionary<string, List<string>>?)e.Keywords)
            .HasJsonbConversionWithComparer<Dictionary<string, List<string>>>();
        builder.HasIndex(p => p.Language)
            .HasFilter("is_deleted = false")
            .IsUnique();
    }
}
