// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Domain.Interfaces.Associations;

/// <summary>
/// Repository for <see cref="Qualification"/> master entities.
/// </summary>
public interface IQualificationRepository : IBaseRepository<Qualification>
{
    /// <summary>
    /// Finds by German (de) name, case-insensitive.
    /// </summary>
    Task<Qualification?> GetByNameAsync(string name, CancellationToken ct = default);

    Task<List<Qualification>> GetAllAsync(CancellationToken ct = default);

    Task<List<Qualification>> GetSelectableAsync(IReadOnlyCollection<string> activeIndustrySlugs, CancellationToken ct = default);

    /// <summary>
    /// Returns all non-deleted qualifications for one industry slug, ordered by name - used by the
    /// industry template preview, independent of the current ACTIVE_INDUSTRIES setting.
    /// </summary>
    Task<List<Qualification>> GetByIndustryAsync(string industry, CancellationToken ct = default);
}
