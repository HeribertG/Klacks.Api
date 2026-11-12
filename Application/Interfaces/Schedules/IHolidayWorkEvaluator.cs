// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reports work placed on a day the client's calendar marks as a statutory holiday, unless an
/// exemption covers that client.
/// </summary>

using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Interfaces.Schedules;

public interface IHolidayWorkEvaluator
{
    /// <summary>
    /// Judges every day in [from, to] on which the client has work.
    /// </summary>
    /// <param name="clientId">The client whose calendar and contract decide holiday and exemption</param>
    /// <param name="clientName">Display name carried into the reported entries</param>
    /// <param name="workDates">Days the client actually has work on, so days off are never inspected</param>
    Task<List<ScheduleValidationNotificationDto>> EvaluateAsync(
        Guid clientId,
        string clientName,
        IReadOnlyCollection<DateOnly> workDates,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The evaluation for several clients in one call: per client the entries of EvaluateAsync with an empty client
    /// name, in the order of <paramref name="clients"/>. The contract data is read once per distinct first-to-last
    /// day range, the exemptions and the enforcement mode once, and a calendar once per calendar selection and
    /// year. A client without work dates reports nothing.
    /// </summary>
    /// <param name="clients">One entry per client with the days it has work on</param>
    Task<List<ScheduleValidationNotificationDto>> EvaluateForClientsAsync(
        IReadOnlyList<ClientWorkDates> clients,
        CancellationToken cancellationToken = default);
}
