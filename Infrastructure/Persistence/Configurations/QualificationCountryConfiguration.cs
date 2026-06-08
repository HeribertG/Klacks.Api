// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the QualificationCountry junction table.
/// </summary>

using Klacks.Api.Domain.Models.Staffs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class QualificationCountryConfiguration : IEntityTypeConfiguration<QualificationCountry>
{
    public void Configure(EntityTypeBuilder<QualificationCountry> builder)
    {
        builder.ToTable("qualification_country");
        builder.HasKey(qc => new { qc.QualificationId, qc.CountryCode });
        builder.HasOne(qc => qc.Qualification)
            .WithMany(q => q.QualificationCountries)
            .HasForeignKey(qc => qc.QualificationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
