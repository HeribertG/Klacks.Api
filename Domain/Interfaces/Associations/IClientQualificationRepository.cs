// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Domain.Interfaces.Associations;

public interface IClientQualificationRepository : IBaseRepository<ClientQualification>
{
    Task<ClientQualification?> GetActiveAsync(Guid clientId, Guid qualificationId, CancellationToken ct = default);

    Task<List<ClientQualification>> GetByClientIdsAsync(IReadOnlyCollection<Guid> clientIds, CancellationToken ct = default);
}
