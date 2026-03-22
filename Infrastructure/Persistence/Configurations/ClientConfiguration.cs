// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the Client entity including sequence, query filter, indexes and relationships.
/// </summary>
using Klacks.Api.Domain.Models.Staffs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.Property(e => e.IdNumber)
              .HasDefaultValueSql("nextval('public.client_idnumber_seq')");

        builder.HasIndex(p => new { p.IsDeleted, p.Name, p.FirstName });
        builder.HasIndex(p => new { p.IsDeleted, p.FirstName, p.Name });
        builder.HasIndex(p => new { p.IsDeleted, p.Company, p.Name });
        builder.HasIndex(p => new { p.IsDeleted, p.IdNumber });
        builder.HasIndex(p => new { p.FirstName, p.SecondName, p.Name, p.MaidenName, p.Company, p.Gender, p.Type, p.LegalEntity, p.IsDeleted });

        builder.HasMany(c => c.Addresses)
            .WithOne(a => a.Client)
            .HasForeignKey(a => a.ClientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Communications)
            .WithOne(a => a.Client)
            .HasForeignKey(a => a.ClientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Annotations)
            .WithOne(a => a.Client)
            .HasForeignKey(a => a.ClientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.GroupItems)
            .WithOne(a => a.Client)
            .HasForeignKey(a => a.ClientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.ClientContracts)
            .WithOne(cc => cc.Client)
            .HasForeignKey(cc => cc.ClientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.ClientImage)
            .WithOne(ci => ci.Client)
            .HasForeignKey<ClientImage>(ci => ci.ClientId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Works)
            .WithOne(a => a.Client)
            .HasForeignKey(a => a.ClientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
