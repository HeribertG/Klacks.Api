// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Text.Json.Serialization;

namespace Klacks.Api.Application.DTOs.Clients;

/// <summary>
/// The deterministic findings the employee exit advisor can raise. New members are appended at the end.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<EmployeeExitFindingCode>))]
public enum EmployeeExitFindingCode
{
    NoActiveMembership = 0,
    MultipleActiveMemberships = 1,
    ExitBeforeMembershipStart = 2,
    MembershipAlreadyEndsOnExitDate = 3,
    SealedEntriesAfterExit = 4,
    FuturePlanAfterExit = 5,
    AbsencesAfterExit = 6,
    OnCallAfterExit = 7,
    ContractRunsPastExit = 8,
    ContractEndedBeforeExit = 9,
    GroupMembershipsRemain = 10
}
