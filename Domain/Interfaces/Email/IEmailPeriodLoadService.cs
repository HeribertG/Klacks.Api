// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces.Email;

public interface IEmailPeriodLoadService
{
    Task<string?> BuildSummaryAsync(Guid clientId, DateOnly fromDate, DateOnly untilDate, CancellationToken cancellationToken = default);
}
