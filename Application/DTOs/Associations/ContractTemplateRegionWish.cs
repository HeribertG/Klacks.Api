// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The holiday-calendar region an administrator wishes for a new contract, already resolved against the
/// installation's calendar selections so the comparison stays free of repository access.
/// </summary>
/// <param name="Code">State, canton or region code as the administrator stated it</param>
/// <param name="CalendarSelectionIds">Ids of every calendar selection that covers this region in the installation's default country; empty when none does</param>
namespace Klacks.Api.Application.DTOs.Associations;

public sealed record ContractTemplateRegionWish(string Code, IReadOnlySet<Guid> CalendarSelectionIds);
