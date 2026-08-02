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
        builder.Property(e => (List<ReportTemplateVersion>?)e.Versions).HasJsonbListConversion<ReportTemplateVersion>();
        builder.Property(e => (List<ReportParameter>?)e.Parameters).HasJsonbListConversion<ReportParameter>();
        builder.HasIndex(p => new { p.IsDeleted, p.Type, p.Name });
    }
}
