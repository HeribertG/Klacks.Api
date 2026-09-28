// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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
