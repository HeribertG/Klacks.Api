// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the WizardRunCaptureWork child link: soft-delete query filter and a
/// capture-scoped index so the measurement job can fetch a capture's created works efficiently.
/// </summary>

using Klacks.Api.Domain.Models.Schedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class WizardRunCaptureWorkConfiguration : IEntityTypeConfiguration<WizardRunCaptureWork>
{
    public void Configure(EntityTypeBuilder<WizardRunCaptureWork> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => new { p.IsDeleted, p.CaptureId });
        builder.HasIndex(p => p.WorkId);
    }
}
