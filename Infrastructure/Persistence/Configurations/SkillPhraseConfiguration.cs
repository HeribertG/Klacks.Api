// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the SkillPhrase entity with table name, query filter, defaults and indexes.
/// The unique index uses NULLS NOT DISTINCT because Language is null on every keyword row: with the
/// PostgreSQL default of distinct nulls the index would not guard keywords against duplicates at all.
/// </summary>
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class SkillPhraseConfiguration : IEntityTypeConfiguration<SkillPhrase>
{
    private const double DefaultWeight = 1.0;

    public void Configure(EntityTypeBuilder<SkillPhrase> builder)
    {
        builder.ToTable("skill_phrase");
        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.Property(p => p.Weight).HasDefaultValue(DefaultWeight);
        builder.Property(p => p.Status).HasDefaultValue(SkillPhraseStatuses.Active);

        builder.HasIndex(p => new { p.OwnerKind, p.OwnerName });

        builder.HasIndex(p => new { p.OwnerKind, p.OwnerName, p.Language, p.Kind, p.Phrase })
            .HasFilter("is_deleted = false")
            .AreNullsDistinct(false)
            .IsUnique();
    }
}
