// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists a user's absence periods for the escalation roster card's admin editor.
/// </summary>
/// <param name="repository">Reads UserAbsencePeriod rows for the requested user.</param>

using Klacks.Api.Application.DTOs.Accounts;
using Klacks.Api.Application.Queries.Accounts;
using Klacks.Api.Domain.Interfaces.Authentification;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Accounts;

public class GetUserAbsencePeriodsQueryHandler : IRequestHandler<GetUserAbsencePeriodsQuery, IReadOnlyList<UserAbsencePeriodResource>>
{
    private readonly IUserAbsencePeriodRepository _repository;

    public GetUserAbsencePeriodsQueryHandler(IUserAbsencePeriodRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<UserAbsencePeriodResource>> Handle(GetUserAbsencePeriodsQuery request, CancellationToken cancellationToken)
    {
        var periods = await _repository.GetByUserIdAsync(request.AppUserId, cancellationToken);

        return periods.Select(p => new UserAbsencePeriodResource
        {
            Id = p.Id,
            AppUserId = p.AppUserId,
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            Reason = p.Reason
        }).ToList();
    }
}
