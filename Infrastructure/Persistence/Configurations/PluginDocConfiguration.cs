// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core Konfiguration fuer die PluginDoc-Entity mit eindeutigem Index.
/// </summary>
using Klacks.Api.Domain.Models.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class PluginDocConfiguration : IEntityTypeConfiguration<PluginDoc>
{
    public void Configure(EntityTypeBuilder<PluginDoc> builder)
    {
        builder.HasIndex(p => new { p.PluginCode, p.ManualName }).IsUnique();
    }
}
