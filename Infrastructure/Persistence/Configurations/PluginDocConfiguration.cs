// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the PluginDoc-Entity with unique index.
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
