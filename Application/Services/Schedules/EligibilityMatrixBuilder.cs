// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Precomputes which (agent, shift, date) assignments are ineligible because the agent lacks a
/// mandatory qualification the shift requires (date- and level-aware via <see cref="EligibilityMatcher"/>).
/// Shared by both wizard context builders so all three wizards veto on one source of truth.
/// </summary>
/// <param name="clientQualificationRepository">Loads the qualifications held by the planning agents</param>
/// <param name="shiftRequiredQualificationRepository">Loads the qualifications each shift requires</param>

using Klacks.Api.Application.Interfaces.Schedules;
using System.Globalization;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Associations;
using Klacks.ScheduleOptimizer.Models;

namespace Klacks.Api.Application.Services.Schedules;

public sealed class EligibilityMatrixBuilder : IEligibilityMatrixBuilder
{
    private readonly IClientQualificationRepository _clientQualificationRepository;
    private readonly IShiftRequiredQualificationRepository _shiftRequiredQualificationRepository;

    public EligibilityMatrixBuilder(
        IClientQualificationRepository clientQualificationRepository,
        IShiftRequiredQualificationRepository shiftRequiredQualificationRepository)
    {
        _clientQualificationRepository = clientQualificationRepository;
        _shiftRequiredQualificationRepository = shiftRequiredQualificationRepository;
    }

    public async Task<EligibilityMatrix> BuildAsync(
        IReadOnlyCollection<Guid> agentIds,
        IReadOnlyCollection<EligibilitySlot> slots,
        CancellationToken ct = default)
    {
        if (agentIds.Count == 0 || slots.Count == 0)
        {
            return EligibilityMatrix.Empty;
        }

        var shiftIds = slots.Select(s => s.ShiftId).Distinct().ToList();

        var requiredByShift = (await _shiftRequiredQualificationRepository.GetByShiftIdsAsync(shiftIds, ct))
            .GroupBy(r => r.ShiftId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<ShiftRequiredQualification>)g.ToList());

        // Any shift carrying a required qualification (mandatory OR optional) can produce a gap:
        // a missing mandatory one is an Error (veto), everything else a Warning (report only).
        if (requiredByShift.Count == 0)
        {
            return EligibilityMatrix.Empty;
        }

        var gatedShiftIds = requiredByShift.Keys.ToHashSet();

        var heldByClient = (await _clientQualificationRepository.GetByClientIdsAsync(agentIds, ct))
            .GroupBy(q => q.ClientId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<ClientQualification>)g.ToList());

        var qualificationInfo = BuildQualificationInfo(requiredByShift, gatedShiftIds);

        var ineligible = new HashSet<(string, Guid, DateOnly)>();
        var gaps = new Dictionary<(string, Guid, DateOnly), IReadOnlyList<QualificationGap>>();
        IReadOnlyList<ClientQualification> noQuals = [];

        foreach (var slot in slots.Where(s => gatedShiftIds.Contains(s.ShiftId)))
        {
            var requirements = requiredByShift[slot.ShiftId];
            foreach (var agentId in agentIds)
            {
                var held = heldByClient.GetValueOrDefault(agentId, noQuals);
                var slotGaps = EligibilityMatcher.FindGaps(requirements, held, slot.Date);
                if (slotGaps.Count == 0)
                {
                    continue;
                }

                var key = (agentId.ToString(), slot.ShiftId, slot.Date);
                gaps[key] = slotGaps;

                // Only a hard Error gap blocks the assignment; Warning gaps stay assignable but reported.
                if (slotGaps.Any(g => g.Severity == QualificationGapSeverity.Error))
                {
                    ineligible.Add(key);
                }
            }
        }

        return new EligibilityMatrix
        {
            Ineligible = ineligible,
            Gaps = gaps,
            QualificationInfo = qualificationInfo,
            ShiftNames = BuildShiftNames(requiredByShift, gatedShiftIds),
        };
    }

    /// <summary>
    /// Derives the distinct (shift, date) slots from the already expanded CoreShift list so both the
    /// context builder and the result/report builder feed the matrix from one source.
    /// </summary>
    public static IReadOnlyList<EligibilitySlot> SlotsFromShifts(IReadOnlyList<CoreShift> shifts)
    {
        var set = new HashSet<EligibilitySlot>();
        foreach (var shift in shifts)
        {
            if (Guid.TryParse(shift.Id, out var shiftId)
                && DateOnly.TryParseExact(shift.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                set.Add(new EligibilitySlot(shiftId, date));
            }
        }

        return set.ToList();
    }

    private static IReadOnlyDictionary<Guid, QualificationInfo> BuildQualificationInfo(
        IReadOnlyDictionary<Guid, IReadOnlyList<ShiftRequiredQualification>> requiredByShift,
        IReadOnlySet<Guid> gatedShiftIds)
    {
        var info = new Dictionary<Guid, QualificationInfo>();
        foreach (var shiftId in gatedShiftIds)
        {
            foreach (var req in requiredByShift[shiftId])
            {
                if (req.Qualification is null || info.ContainsKey(req.QualificationId))
                {
                    continue;
                }

                info[req.QualificationId] = new QualificationInfo(req.Qualification.Name, req.Qualification.Emoji);
            }
        }

        return info;
    }

    private static IReadOnlyDictionary<Guid, string> BuildShiftNames(
        IReadOnlyDictionary<Guid, IReadOnlyList<ShiftRequiredQualification>> requiredByShift,
        IReadOnlySet<Guid> gatedShiftIds)
    {
        var names = new Dictionary<Guid, string>();
        foreach (var shiftId in gatedShiftIds)
        {
            var shift = requiredByShift[shiftId].Select(r => r.Shift).FirstOrDefault(s => s is not null);
            if (shift is not null)
            {
                names[shiftId] = string.IsNullOrWhiteSpace(shift.Name) ? shift.Abbreviation : shift.Name;
            }
        }

        return names;
    }
}
