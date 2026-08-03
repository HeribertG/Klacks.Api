// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the SkillPhrase entity with table name, query filter, defaults and indexes.
/// Language is required: a phrase carries either an ISO tag or one of the reserved tags in
/// SkillPhraseLanguages, which is what makes "language-neutral" a value the index can be filtered on.
/// The unique index therefore no longer needs NULLS NOT DISTINCT to guard keyword rows.
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
        builder.Property(p => p.Language).IsRequired().HasDefaultValue(SkillPhraseLanguages.Undetermined);

        builder.HasIndex(p => new { p.OwnerKind, p.OwnerName });

        builder.HasIndex(p => new { p.OwnerKind, p.OwnerName, p.Language, p.Kind, p.Phrase })
            .HasFilter("is_deleted = false")
            .IsUnique();
    }
}
