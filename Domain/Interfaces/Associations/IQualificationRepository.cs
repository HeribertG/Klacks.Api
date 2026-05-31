// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Domain.Interfaces.Associations;

public interface IQualificationRepository : IBaseRepository<Qualification>
{
    Task<Qualification?> GetByNameAsync(string name, CancellationToken ct = default);

    Task<List<Qualification>> GetAllAsync(CancellationToken ct = default);
}
