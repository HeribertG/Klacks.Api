// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for SkillLearningCluster. The partial unique index on (agent_id, cluster_key)
/// is what makes the collector's insert-then-handle-23505 upsert safe across API instances; without it
/// two instances handling the same utterance at the same moment would create two competing clusters.
/// The embedding column is not mapped here: pgvector columns are created and queried through raw SQL in
/// this project (see KnowledgeIndexRepository), never through EF.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class SkillLearningClusterConfiguration : IEntityTypeConfiguration<SkillLearningCluster>
{
    private const int ClusterKeyMaxLength = 64;
    private const int IntentExcerptMaxLength = 120;
    private const int LocaleMaxLength = 8;
    private const int StatusMaxLength = 24;
    private const int OutcomeRefKindMaxLength = 16;
    private const int OutcomeRefMaxLength = 128;
    private const int LearningInstanceMaxLength = 64;

    private const string ActiveRowFilter = "\"is_deleted\" = false";

    public void Configure(EntityTypeBuilder<SkillLearningCluster> builder)
    {
        builder.ToTable("skill_learning_clusters");
        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.Property(p => p.ClusterKey).HasMaxLength(ClusterKeyMaxLength).IsRequired();
        builder.Property(p => p.IntentExcerpt).HasMaxLength(IntentExcerptMaxLength);
        builder.Property(p => p.Locale).HasMaxLength(LocaleMaxLength);
        builder.Property(p => p.Status).HasMaxLength(StatusMaxLength).IsRequired();
        builder.Property(p => p.OutcomeRefKind).HasMaxLength(OutcomeRefKindMaxLength);
        builder.Property(p => p.OutcomeRef).HasMaxLength(OutcomeRefMaxLength);
        builder.Property(p => p.LearningInstance).HasMaxLength(LearningInstanceMaxLength);
        builder.Property(p => p.SignalKindsJson).HasColumnType("jsonb");

        builder.HasIndex(p => new { p.AgentId, p.ClusterKey })
            .IsUnique()
            .HasFilter(ActiveRowFilter);
        builder.HasIndex(p => new { p.AgentId, p.Status });
        builder.HasIndex(p => new { p.Status, p.LastSeenAtUtc });
        builder.HasIndex(p => new { p.Status, p.StatusChangedAtUtc });
    }
}
