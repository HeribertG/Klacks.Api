// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core Konfiguration fuer die HeartbeatConfig-Entity mit QueryFilter und Indizes.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class HeartbeatConfigConfiguration : IEntityTypeConfiguration<HeartbeatConfig>
{
    public void Configure(EntityTypeBuilder<HeartbeatConfig> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => new { p.IsDeleted, p.UserId });
        builder.HasIndex(p => new { p.IsDeleted, p.IsEnabled });
    }
}
