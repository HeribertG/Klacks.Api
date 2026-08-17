// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Records a user's absence period, so the escalation roster skips them while it is active.
/// </summary>
/// <param name="repository">Persists the UserAbsencePeriod row.</param>

using Klacks.Api.Application.Commands.Accounts;
using Klacks.Api.Application.DTOs.Accounts;
using Klacks.Api.Domain.Interfaces.Authentification;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Accounts;

public class CreateUserAbsencePeriodCommandHandler : IRequestHandler<CreateUserAbsencePeriodCommand, UserAbsencePeriodResource>
{
    private readonly IUserAbsencePeriodRepository _repository;

    public CreateUserAbsencePeriodCommandHandler(IUserAbsencePeriodRepository repository)
    {
        _repository = repository;
    }

    public async Task<UserAbsencePeriodResource> Handle(CreateUserAbsencePeriodCommand request, CancellationToken cancellationToken)
    {
        var period = new UserAbsencePeriod
        {
            Id = Guid.NewGuid(),
            AppUserId = request.AppUserId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Reason = request.Reason
        };

        var added = await _repository.AddAsync(period, cancellationToken);

        return new UserAbsencePeriodResource
        {
            Id = added.Id,
            AppUserId = added.AppUserId,
            StartDate = added.StartDate,
            EndDate = added.EndDate,
            Reason = added.Reason
        };
    }
}
