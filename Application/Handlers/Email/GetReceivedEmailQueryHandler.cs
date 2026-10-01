// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads one received email. An email whose sender belongs only to clients outside the caller's group
/// visibility is answered exactly like a missing email.
/// </summary>
/// <param name="emailQueryRepository">Resolves the clients that own the sender address</param>
/// <param name="clientVisibilityGuard">Decides whether the calling user may see those clients</param>

using Klacks.Api.Application.DTOs.Email;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.Email;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Email;

public class GetReceivedEmailQueryHandler : BaseHandler, IRequestHandler<GetReceivedEmailQuery, ReceivedEmailResource?>
{
    private readonly IReceivedEmailRepository _repository;
    private readonly IEmailQueryRepository _emailQueryRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly ReceivedEmailMapper _mapper;

    public GetReceivedEmailQueryHandler(
        IReceivedEmailRepository repository,
        IEmailQueryRepository emailQueryRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        ReceivedEmailMapper mapper,
        ILogger<GetReceivedEmailQueryHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _emailQueryRepository = emailQueryRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _mapper = mapper;
    }

    public async Task<ReceivedEmailResource?> Handle(GetReceivedEmailQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var email = await _repository.GetByIdAsync(request.Id);
            if (email == null
                || await ReceivedEmailVisibility.IsHiddenAsync(_emailQueryRepository, _clientVisibilityGuard, email, cancellationToken))
            {
                return null;
            }

            return _mapper.ToResource(email);
        }, nameof(GetReceivedEmailQuery), new { request.Id });
    }
}
