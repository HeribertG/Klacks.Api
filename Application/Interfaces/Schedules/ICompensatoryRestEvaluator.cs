// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Read path for K12 stage-1 compensatory-rest tracking: reports every open (unfulfilled) obligation of
/// one client as a feed entry — "due" while inside its deadline, "overdue" once the deadline has passed —
/// escalating to Error when the compensatoryRest compliance rule is in Block mode. Does not write; the
/// obligations themselves are produced by the reconciler. Real mode only.
/// </summary>

using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Interfaces.Schedules;

public interface ICompensatoryRestEvaluator
{
    Task<List<ScheduleValidationNotificationDto>> EvaluateAsync(
        Guid clientId,
        string clientName,
        DateOnly asOfDate,
        Guid? analyseToken = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The evaluation for several clients in one call: per client the entries of EvaluateAsync with an empty client
    /// name, in the order of <paramref name="clients"/>. The enabled flag, the open obligations and the enforcement
    /// mode are read for all clients together.
    /// </summary>
    /// <param name="clients">One entry per client with the day its obligations are judged on</param>
    /// <param name="analyseToken">Scenario token; any value other than null reports nothing</param>
    Task<List<ScheduleValidationNotificationDto>> EvaluateForClientsAsync(
        IReadOnlyList<ClientAsOfDate> clients,
        Guid? analyseToken = null,
        CancellationToken cancellationToken = default);
}
