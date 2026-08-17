// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Removes a user's absence period, e.g. when a holiday was entered by mistake or ends early.
/// </summary>
/// <param name="repository">Deletes the UserAbsencePeriod row.</param>

using Klacks.Api.Application.Commands.Accounts;
using Klacks.Api.Domain.DTOs;
using Klacks.Api.Domain.Interfaces.Authentification;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Accounts;

public class DeleteUserAbsencePeriodCommandHandler : IRequestHandler<DeleteUserAbsencePeriodCommand, HttpResultResource>
{
    private readonly IUserAbsencePeriodRepository _repository;

    public DeleteUserAbsencePeriodCommandHandler(IUserAbsencePeriodRepository repository)
    {
        _repository = repository;
    }

    public async Task<HttpResultResource> Handle(DeleteUserAbsencePeriodCommand request, CancellationToken cancellationToken)
    {
        var deleted = await _repository.DeleteAsync(request.Id, cancellationToken);

        return new HttpResultResource
        {
            Success = deleted,
            Messages = deleted ? "Absence period deleted" : "Absence period not found"
        };
    }
}
