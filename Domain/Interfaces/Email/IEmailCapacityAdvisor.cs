// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Email;

namespace Klacks.Api.Domain.Interfaces.Email;

public interface IEmailCapacityAdvisor
{
    Task<EmailCapacityVerdict> JudgeAsync(
        Guid clientId,
        DateOnly fromDate,
        DateOnly untilDate,
        double requestedDailyValue,
        CancellationToken cancellationToken = default);
}
