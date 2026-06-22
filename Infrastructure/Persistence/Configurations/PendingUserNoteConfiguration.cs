// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the PendingUserNote entity: soft-delete query filter,
/// lookup index per agent and user, and the agent foreign key.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class PendingUserNoteConfiguration : IEntityTypeConfiguration<PendingUserNote>
{
    public void Configure(EntityTypeBuilder<PendingUserNote> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => new { p.AgentId, p.UserId, p.IsDeleted });

        builder.Property(p => p.Content).IsRequired();

        builder.HasOne<Agent>()
            .WithMany()
            .HasForeignKey(p => p.AgentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
