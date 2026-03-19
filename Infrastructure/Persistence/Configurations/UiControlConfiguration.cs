// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core Konfiguration fuer die UiControl-Entity mit QueryFilter, Indizes und Parent-Child-Beziehung.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class UiControlConfiguration : IEntityTypeConfiguration<UiControl>
{
    public void Configure(EntityTypeBuilder<UiControl> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => new { p.PageKey, p.ControlKey })
            .HasFilter("is_deleted = false")
            .IsUnique();
        builder.HasIndex(p => new { p.PageKey, p.SortOrder })
            .HasFilter("is_deleted = false");

        builder.HasOne(c => c.ParentControl)
            .WithMany(c => c.ChildControls)
            .HasForeignKey(c => c.ParentControlId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
