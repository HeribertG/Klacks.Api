// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for persisted terminal job outcomes. The unique index on JobId carries the
/// "first terminal state wins" rule: a late failure path arriving after a completion loses the insert
/// instead of overwriting it, so a finished run can never be reported as failed.
/// </summary>

using Klacks.Api.Domain.Models.Schedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class JobTerminalStateRowConfiguration : IEntityTypeConfiguration<JobTerminalStateRow>
{
    public void Configure(EntityTypeBuilder<JobTerminalStateRow> builder)
    {
        builder.ToTable("job_terminal_states");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Status).HasMaxLength(32);
        builder.HasIndex(p => p.JobId).IsUnique();
        builder.HasIndex(p => p.ExpiresAtUtc);
    }
}
