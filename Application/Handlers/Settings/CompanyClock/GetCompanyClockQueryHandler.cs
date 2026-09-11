// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves the company's time zone and current calendar date through ICompanyClock's single
/// resolution chain and memo, so this read-only endpoint never diverges from the rest of the backend
/// (business dates, schedule calculations, ...) about what "today" or "the company zone" is.
/// </summary>
/// <param name="companyClock">Resolves the company's configured time zone, its source, and today's date.</param>

using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Application.Queries.Settings.CompanyClock;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Settings.CompanyClock;

public class GetCompanyClockQueryHandler : IRequestHandler<GetCompanyClockQuery, CompanyClockResource>
{
    private readonly ICompanyClock _companyClock;

    public GetCompanyClockQueryHandler(ICompanyClock companyClock)
    {
        _companyClock = companyClock;
    }

    public async Task<CompanyClockResource> Handle(GetCompanyClockQuery request, CancellationToken cancellationToken)
    {
        var resolution = await _companyClock.GetTimeZoneResolutionAsync(cancellationToken);
        var today = await _companyClock.GetTodayDateAsync(cancellationToken);

        return new CompanyClockResource
        {
            TimeZone = resolution.IanaId,
            Today = today,
            Source = resolution.Source.ToString(),
        };
    }
}
