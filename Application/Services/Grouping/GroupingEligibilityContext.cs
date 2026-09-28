// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Pre-indexed inputs of the static eligibility evaluation, built once per analysis so that every
/// (client, shift, day) check is a dictionary lookup.
/// </summary>
/// <param name="Shifts">Analysed plannable shifts by id (times feed the shift-type and availability rules).</param>
/// <param name="RunDays">Days in the period on which each shift runs, ascending (from IWizardShiftBuilder).</param>
/// <param name="Contracts">Effective contract data per day and client (IClientContractDataProvider range call).</param>
/// <param name="RequirementsByShift">Required qualifications per shift.</param>
/// <param name="QualificationsByClient">Held qualifications per client.</param>
/// <param name="Blacklist">(client, shift) pairs the client refuses.</param>
/// <param name="AvailabilityByClientAndDay">Hourly availability entries per client and day.</param>
/// <param name="ExpiredMandatoryBlocks">Value of QUALIFICATION_EXPIRED_MANDATORY_BLOCKS.</param>

using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Application.Services.Grouping;

public sealed record GroupingEligibilityContext(
    IReadOnlyDictionary<Guid, GroupingShiftRecord> Shifts,
    IReadOnlyDictionary<Guid, IReadOnlyList<DateOnly>> RunDays,
    IReadOnlyDictionary<DateOnly, IReadOnlyDictionary<Guid, EffectiveContractData>> Contracts,
    IReadOnlyDictionary<Guid, IReadOnlyList<ShiftRequiredQualification>> RequirementsByShift,
    IReadOnlyDictionary<Guid, IReadOnlyList<ClientQualification>> QualificationsByClient,
    IReadOnlySet<GroupingEntityPair> Blacklist,
    IReadOnlyDictionary<(Guid ClientId, DateOnly Day), IReadOnlyList<ClientAvailability>> AvailabilityByClientAndDay,
    bool ExpiredMandatoryBlocks);
