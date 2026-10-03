// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// EF Core configuration for PlanningConstraint (table planning_constraint). Enums are stored as integers,
/// ParametersJson as jsonb. Indexes: the import key is unique among active imported rows only (customer rows
/// carry the empty string, as for CounterRule); (approval_status, valid_from, valid_until) over active rows
/// serves the loader's period query; (scope_type, scope_id) serves scope lookups such as "constraints of this
/// group"; analyse_token serves scenario cleanup and the scenario filter. Optimistic concurrency uses the
/// PostgreSQL system column xmin as a shadow row version (no physical column, no domain property): a concurrent
/// approve/update/expiry makes the losing save fail instead of silently overwriting a decision.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class PlanningConstraintConfiguration : IEntityTypeConfiguration<PlanningConstraint>
{
    private const string JsonbColumnType = "jsonb";
    private const string ActiveRowFilter = "is_deleted = false";
    private const string ActiveImportedRowFilter = "is_deleted = false AND import_source_key <> ''";
    private const string RowVersionProperty = "RowVersion";
    private const string PostgresRowVersionColumn = "xmin";

    public void Configure(EntityTypeBuilder<PlanningConstraint> builder)
    {
        builder.Property(c => c.Kind).HasConversion<int>();
        builder.Property(c => c.Severity).HasConversion<int>();
        builder.Property(c => c.ScopeType).HasConversion<int>();
        builder.Property(c => c.Origin).HasConversion<int>();
        builder.Property(c => c.ApprovalStatus).HasConversion<int>();

        builder.Property<uint>(RowVersionProperty).IsRowVersion().HasColumnName(PostgresRowVersionColumn);

        builder.Property(c => c.ParametersJson).IsRequired().HasColumnType(JsonbColumnType);
        builder.Property(c => c.SourceText).HasMaxLength(PlanningConstraintDefaults.SourceTextMaxLength);
        builder.Property(c => c.Paraphrase).HasMaxLength(PlanningConstraintDefaults.ParaphraseMaxLength);
        builder.Property(c => c.ProposedBy).HasMaxLength(PlanningConstraintDefaults.ActorMaxLength);
        builder.Property(c => c.ApprovedBy).HasMaxLength(PlanningConstraintDefaults.ActorMaxLength);

        builder.Property(c => c.ImportSourceKey).IsRequired().HasMaxLength(PlanningConstraintDefaults.ImportSourceKeyMaxLength).HasDefaultValue(string.Empty);
        builder.Property(c => c.ImportContentHash).IsRequired().HasMaxLength(PlanningConstraintDefaults.ImportContentHashMaxLength).HasDefaultValue(string.Empty);

        builder.HasIndex(c => c.ImportSourceKey)
            .IsUnique()
            .HasFilter(ActiveImportedRowFilter);

        builder.HasIndex(c => new { c.ApprovalStatus, c.ValidFrom, c.ValidUntil })
            .HasFilter(ActiveRowFilter);

        builder.HasIndex(c => new { c.ScopeType, c.ScopeId });

        builder.HasIndex(c => c.AnalyseToken);
    }
}
