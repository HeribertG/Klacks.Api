// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the HeartbeatConfig-Entity with query filter and indexes. One user owns
/// at most one live configuration: the heartbeat loop and the configure skill both look one up and
/// create it when absent, so the unique index on UserId is what stops two concurrent callers -- a
/// second API instance, or an overlapping tick -- from each inserting one and leaving the winner to
/// whichever row FirstOrDefault happens to return.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class HeartbeatConfigConfiguration : IEntityTypeConfiguration<HeartbeatConfig>
{
    public void Configure(EntityTypeBuilder<HeartbeatConfig> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => new { p.IsDeleted, p.UserId });
        builder.HasIndex(p => new { p.IsDeleted, p.IsEnabled });
        builder.HasIndex(p => p.UserId).IsUnique().HasFilter("\"is_deleted\" = false");
    }
}
