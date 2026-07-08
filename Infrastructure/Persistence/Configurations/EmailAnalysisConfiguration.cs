// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the EmailAnalysis entity: one analysis per received email,
/// with lookup indexes on the resolved client and the detected intent.
/// </summary>
using Klacks.Api.Domain.Models.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class EmailAnalysisConfiguration : IEntityTypeConfiguration<EmailAnalysis>
{
    public void Configure(EntityTypeBuilder<EmailAnalysis> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => p.ReceivedEmailId).IsUnique();
        builder.HasIndex(p => new { p.IsDeleted, p.ClientId });
        builder.HasIndex(p => new { p.IsDeleted, p.Intent });

        builder.HasOne(p => p.ReceivedEmail)
            .WithMany()
            .HasForeignKey(p => p.ReceivedEmailId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
