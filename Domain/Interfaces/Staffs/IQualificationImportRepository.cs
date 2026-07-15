// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persistence access for the region-setup entity-import path (K20) over
/// <see cref="Klacks.Api.Domain.Models.Staffs.Qualification"/> rows: lookup by natural import keys for
/// the re-apply reconciliation, plus the insert/update primitives the import decisions execute.
/// Customer CRUD goes through the regular qualification repository, never through this one.
/// </summary>

using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Domain.Interfaces.Staffs;

public interface IQualificationImportRepository
{
    Task<List<Qualification>> GetBySourceKeysAsync(IReadOnlyCollection<string> sourceKeys);

    void Add(Qualification qualification);

    void Update(Qualification qualification);
}
