// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the ClientScheduleDetail-Entity with Index.
/// </summary>
using Klacks.Api.Domain.Models.Schedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class ClientScheduleDetailConfiguration : IEntityTypeConfiguration<ClientScheduleDetail>
{
    public void Configure(EntityTypeBuilder<ClientScheduleDetail> builder)
    {
        builder.HasIndex(p => new { p.ClientId, p.CurrentYear, p.CurrentMonth });
    }
}
