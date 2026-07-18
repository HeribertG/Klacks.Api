// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// A named qualification / skill an employee can hold and a shift can require
/// (e.g. "Forklift licence", "First aid"). Soft-deletable master entity.
/// Name and Description are stored as JSONB MultiLanguage values.
/// </summary>
/// <remarks>
/// Type distinguishes between language qualifications and work qualifications.
/// Countries is a many-to-many list of ISO codes stored in the qualification_country junction table.
/// Rows are either customer-created (full CRUD, ImportSourceKey empty) or imported from a region-setup
/// industry-profile qualification catalog (K20, ImportSourceKey/ImportContentHash set — see
/// <see cref="IImportableEntity"/>); an imported row a customer has edited is never overwritten.
/// </remarks>

using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Staffs;

public class Qualification : BaseEntity, IImportableEntity
{
    public MultiLanguage Name { get; set; } = new();

    public MultiLanguage? Description { get; set; }

    public string? Emoji { get; set; }

    public bool IsTimeLimited { get; set; }

    public QualificationType Type { get; set; } = QualificationType.Work;

    public QualificationCategory Category { get; set; } = QualificationCategory.None;

    [JsonIgnore]
    public ICollection<QualificationCountry> QualificationCountries { get; set; } = [];

    private List<string> _inputCountries = [];

    [NotMapped]
    public List<string> Countries
    {
        get => QualificationCountries.Count > 0
            ? [.. QualificationCountries.Select(c => c.CountryCode)]
            : _inputCountries;
        set => _inputCountries = value ?? [];
    }

    public string Industry { get; set; } = string.Empty;

    public string ImportSourceKey { get; set; } = string.Empty;

    public string ImportContentHash { get; set; } = string.Empty;
}
