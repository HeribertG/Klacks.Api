// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// EF configuration for UserIntroTourState: table user_intro_tour_states keyed by the account id, cascading with
/// the hard-deleted AspNetUsers row, completed stations as a jsonb list that defaults to an empty array.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class UserIntroTourStateConfiguration : IEntityTypeConfiguration<UserIntroTourState>
{
    private const string EmptyJsonArraySql = "'[]'::jsonb";

    public void Configure(EntityTypeBuilder<UserIntroTourState> builder)
    {
        builder.ToTable(IntroTourDefaults.TableName);
        builder.HasKey(s => s.UserId);

        builder.Property(s => s.Status)
            .HasMaxLength(IntroTourDefaults.StatusMaxLength)
            .IsRequired();

        builder.Property(s => (List<string>?)s.CompletedStations)
            .HasJsonbConversionWithComparer<List<string>>()
            .HasDefaultValueSql(EmptyJsonArraySql)
            .IsRequired();

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
