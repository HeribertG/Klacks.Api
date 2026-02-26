// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Queries.Email;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Email;

public class GetUnreadEmailCountQueryHandler : BaseHandler, IRequestHandler<GetUnreadEmailCountQuery, int>
{
    private readonly IReceivedEmailRepository _repository;

    public GetUnreadEmailCountQueryHandler(
        IReceivedEmailRepository repository,
        ILogger<GetUnreadEmailCountQueryHandler> logger)
        : base(logger)
    {
        _repository = repository;
    }

    public async Task<int> Handle(GetUnreadEmailCountQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            return await _repository.GetUnreadCountAsync();
        }, nameof(GetUnreadEmailCountQuery));
    }
}
