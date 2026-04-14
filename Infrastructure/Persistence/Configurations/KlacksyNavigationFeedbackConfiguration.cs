// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the KlacksyNavigationFeedback entity, mapping it to the klacksy_navigation_feedback table.
/// </summary>

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

using Klacks.Api.Domain.Models.Klacksy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class KlacksyNavigationFeedbackConfiguration : IEntityTypeConfiguration<KlacksyNavigationFeedback>
{
    public void Configure(EntityTypeBuilder<KlacksyNavigationFeedback> b)
    {
        b.ToTable("klacksy_navigation_feedback");
        b.HasKey(x => x.Id);
        b.Property(x => x.Utterance).HasMaxLength(500).IsRequired();
        b.Property(x => x.Locale).HasMaxLength(10).IsRequired();
        b.Property(x => x.MatchedTargetId).HasMaxLength(100);
        b.Property(x => x.UserAction).HasMaxLength(50).IsRequired();
        b.Property(x => x.ActualRoute).HasMaxLength(200);
        b.Property(x => x.Timestamp).IsRequired();
        b.HasIndex(x => x.Timestamp);
        b.HasIndex(x => x.MatchedTargetId);
    }
}
