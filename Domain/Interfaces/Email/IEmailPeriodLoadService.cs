// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Interfaces.Email;

public interface IEmailPeriodLoadService
{
    Task<string?> BuildSummaryAsync(Guid clientId, DateOnly fromDate, DateOnly untilDate, CancellationToken cancellationToken = default);
}
