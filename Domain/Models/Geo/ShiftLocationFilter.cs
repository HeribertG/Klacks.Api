// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Geo;

/// <summary>
/// Location filter of the shift/order list: the resolved criteria the order location (OrderLocationAddressRule) must
/// match and the caller's client-visibility restriction. A restricted caller only finds orders by location whose
/// customer they may see, so the filter never discloses where a hidden customer lives.
/// </summary>
/// <param name="Criteria">Resolved country/state/city/zip criteria</param>
/// <param name="CustomerVisibleGroupIds">Groups whose customers the caller may see; null = unrestricted</param>
public sealed record ShiftLocationFilter(
    AddressLocationCriteria Criteria,
    IReadOnlyCollection<Guid>? CustomerVisibleGroupIds);
