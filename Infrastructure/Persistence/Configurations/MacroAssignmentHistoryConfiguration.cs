// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the macro assignment history: soft-delete query filter, an index over holder kind and holder
/// id (the "latest switch of this holder" lookup of an undo) and an index over the switch id (all rows of one switch).
/// No foreign keys on purpose (see the entity).
/// </summary>

using Klacks.Api.Domain.Models.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class MacroAssignmentHistoryConfiguration : IEntityTypeConfiguration<MacroAssignmentHistory>
{
    public void Configure(EntityTypeBuilder<MacroAssignmentHistory> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.HasIndex(p => new { p.Target, p.TargetId });

        builder.HasIndex(p => p.SwitchId);
    }
}
