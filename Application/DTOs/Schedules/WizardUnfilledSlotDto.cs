// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// One slot the finished wizard plan leaves understaffed, with the reason.
/// </summary>
/// <param name="ShiftId">Shift GUID as string</param>
/// <param name="Date">ISO date (yyyy-MM-dd) the slot starts on</param>
/// <param name="MissingSeats">Demanded seats minus assigned seats</param>
/// <param name="FeasibleAgentCount">Agents the hard rules still allow on the slot; 0 = no agent may legally take it (unsolvable)</param>
/// <param name="VetoCounts">Hard rule name to number of agents it ruled out; each agent counts once, under its first failing rule in check order</param>
public sealed record WizardUnfilledSlotDto(
    string ShiftId,
    string Date,
    int MissingSeats,
    int FeasibleAgentCount,
    IReadOnlyDictionary<string, int> VetoCounts);