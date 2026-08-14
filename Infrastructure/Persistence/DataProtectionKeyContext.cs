// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Dedicated context for the DataProtection key ring, kept separate from <see cref="DataBaseContext"/>
/// on purpose: that context takes ISettingsEncryptionService, which itself needs the DataProtection
/// provider, so hosting the key ring there would close a dependency cycle. It maps to the same table
/// and the same database, so a single pg_dump still covers the keys.
/// </summary>
/// <param name="options">Connection options pointing at the same database as DataBaseContext</param>

using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Persistence;

public class DataProtectionKeyContext : DbContext, IDataProtectionKeyContext
{
    public DataProtectionKeyContext(DbContextOptions<DataProtectionKeyContext> options)
        : base(options)
    {
    }

    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<DataProtectionKey>(entity =>
        {
            entity.ToTable(DataProtectionKeySchema.TableName);
            entity.Property(k => k.Id).HasColumnName(DataProtectionKeySchema.IdColumn);
            entity.Property(k => k.FriendlyName).HasColumnName(DataProtectionKeySchema.FriendlyNameColumn);
            entity.Property(k => k.Xml).HasColumnName(DataProtectionKeySchema.XmlColumn);
        });
    }
}
