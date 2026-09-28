// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Application.DTOs.Grouping;

public sealed record GroupingFeasibilitySnapshot(
    IReadOnlyList<GroupingGroupRecord> Groups,
    IReadOnlyList<GroupingMembershipRecord> Memberships,
    IReadOnlyList<GroupingClientRecord> Clients,
    IReadOnlyList<GroupingShiftRecord> Shifts,
    IReadOnlyList<ShiftRequiredQualification> Requirements,
    IReadOnlyList<ClientQualification> Qualifications,
    IReadOnlySet<GroupingEntityPair> Blacklist,
    IReadOnlyList<ClientAvailability> Availability,
    IReadOnlySet<GroupingEntityPair> FutureWorkPairs);
