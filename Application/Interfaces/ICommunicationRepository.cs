// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Application.Interfaces;

public interface ICommunicationRepository : IBaseRepository<Communication>
{
    Task<List<CommunicationType>> TypeList();

    Task<List<Communication>> GetClient(Guid id);

    /// <summary>
    /// Phone entries (mobile and fixed line, never the emergency number) of the given employees, read with one
    /// query and without tracking. Visibility is NOT applied here; the caller passes only visible employees.
    /// </summary>
    /// <param name="clientIds">Employees whose phone numbers are needed</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<List<Communication>> GetPhoneEntriesAsync(IReadOnlyCollection<Guid> clientIds, CancellationToken cancellationToken = default);
}
