// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the WizardRunCapture entity: soft-delete query filter, enum-to-byte
/// conversions and lookup indexes used by the reject-link and the deferred measurement job.
/// </summary>

using Klacks.Api.Domain.Models.Schedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class WizardRunCaptureConfiguration : IEntityTypeConfiguration<WizardRunCapture>
{
    public void Configure(EntityTypeBuilder<WizardRunCapture> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.Property(p => p.Engine).HasConversion<byte>();
        builder.Property(p => p.ApplyKind).HasConversion<byte>();
        builder.Property(p => p.Outcome).HasConversion<byte>();

        builder.HasIndex(p => new { p.IsDeleted, p.ScenarioId });
        builder.HasIndex(p => new { p.IsDeleted, p.GroupId, p.PeriodFrom });
        builder.HasIndex(p => new { p.IsDeleted, p.JobId });

        builder.HasMany(p => p.Works)
            .WithOne(w => w.Capture)
            .HasForeignKey(w => w.CaptureId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
