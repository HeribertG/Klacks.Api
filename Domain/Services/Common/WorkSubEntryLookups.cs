// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Common;

public sealed record WorkSubEntryLookups(
    Dictionary<Guid, List<WorkChange>> WorkChanges,
    Dictionary<Guid, List<Expenses>> Expenses,
    Dictionary<(Guid ClientId, DateOnly Date), List<Break>> Breaks,
    IReadOnlySet<Guid>? BreakCarrierWorkIds = null);
