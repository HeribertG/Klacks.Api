// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for InboundAnalysis: one row per analyzed inbound message (email or
/// messenger). Deliberately no FK/navigation to the source row — a messenger message lives in a
/// plugin-owned table with no FK relationship to core tables (the messaging plugin must stay
/// uninstallable without leaving foreign-key cruft), so both channels are treated the same way here:
/// SourceKind+SourceId are plain values, resolved by application code, not by the database.
/// </summary>
using Klacks.Api.Domain.Models.Inbound;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class InboundAnalysisConfiguration : IEntityTypeConfiguration<InboundAnalysis>
{
    public void Configure(EntityTypeBuilder<InboundAnalysis> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => new { p.SourceKind, p.SourceId }).IsUnique();
        builder.HasIndex(p => new { p.IsDeleted, p.ClientId });
        builder.HasIndex(p => new { p.IsDeleted, p.Intent });
    }
}
