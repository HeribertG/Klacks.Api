// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// EF Core configuration for the ScheduleCell entity as keyless view entity.
/// </summary>
using Klacks.Api.Domain.Models.Schedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class ScheduleCellConfiguration : IEntityTypeConfiguration<ScheduleCell>
{
    public void Configure(EntityTypeBuilder<ScheduleCell> builder)
    {
        builder.HasNoKey().ToView(null);
    }
}
