// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the ReportTemplate-Entity with query filter, JSONB properties and Index.
/// </summary>
using Klacks.Api.Domain.Models.Reports;
using Klacks.Api.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class ReportTemplateConfiguration : IEntityTypeConfiguration<ReportTemplate>
{
    public void Configure(EntityTypeBuilder<ReportTemplate> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.Property(e => (ReportPageSetup?)e.PageSetup).HasJsonbConversion<ReportPageSetup>();
        builder.Property(e => (List<ReportSection>?)e.Sections).HasJsonbListConversion<ReportSection>();
        builder.Property(e => (List<string>?)e.DataSetIds).HasJsonbListConversion<string>();
        builder.HasIndex(p => new { p.IsDeleted, p.Type, p.Name });
    }
}
