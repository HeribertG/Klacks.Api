// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// One slot the finished wizard plan leaves understaffed, with the reason. Every agent of the run is counted exactly
/// once: in EligibilityVetoCounts, in PlacementVetoCounts or in PlaceableAgentCount.
/// </summary>
/// <param name="ShiftId">Shift GUID as string</param>
/// <param name="Date">ISO date (yyyy-MM-dd) the slot starts on</param>
/// <param name="MissingSeats">Demanded seats minus assigned seats</param>
/// <param name="EligibleAgentCount">Agents the hard rules allow on the slot given only the fixed facts (locked work,
/// contracts, absences, keywords, qualifications, blacklist, existing work); 0 = unsolvable without changing locked work
/// or master data</param>
/// <param name="PlaceableAgentCount">Eligible agents the hard rules also allow against the finished plan; 0 while
/// EligibleAgentCount is positive = a different plan could fill the slot</param>
/// <param name="EligibilityVetoCounts">Hard rule name to number of agents that are not eligible (first failing rule each)</param>
/// <param name="PlacementVetoCounts">Hard rule name to number of eligible agents the finished plan blocks (first failing rule each)</param>
public sealed record WizardUnfilledSlotDto(
    string ShiftId,
    string Date,
    int MissingSeats,
    int EligibleAgentCount,
    int PlaceableAgentCount,
    IReadOnlyDictionary<string, int> EligibilityVetoCounts,
    IReadOnlyDictionary<string, int> PlacementVetoCounts);
