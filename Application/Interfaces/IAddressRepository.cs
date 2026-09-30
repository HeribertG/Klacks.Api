// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Application.Interfaces;

public interface IAddressRepository : IBaseRepository<Address>
{
    Task<List<Address>> ClienList(Guid id);

    Task<List<Address>> SimpleList(Guid id);

    /// <summary>
    /// Returns, per city name (trimmed, lower-cased), the mean position and count of all geocoded addresses.
    /// </summary>
    Task<List<CityCentroid>> GetCityCentroidsAsync(CancellationToken cancellationToken = default);
}
