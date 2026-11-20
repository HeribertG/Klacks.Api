// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// EF Core configuration for the stable pseudonym numbers: one number per kind and key, and no number twice per kind.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class LLMPseudonymConfiguration : IEntityTypeConfiguration<LLMPseudonym>
{
    public void Configure(EntityTypeBuilder<LLMPseudonym> builder)
    {
        builder.HasIndex(p => new { p.Kind, p.Key }).IsUnique();
        builder.HasIndex(p => new { p.Kind, p.Ordinal }).IsUnique();
    }
}
