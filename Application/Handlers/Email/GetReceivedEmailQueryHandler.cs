// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Email;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.Email;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Email;

public class GetReceivedEmailQueryHandler : BaseHandler, IRequestHandler<GetReceivedEmailQuery, ReceivedEmailResource?>
{
    private readonly IReceivedEmailRepository _repository;
    private readonly ReceivedEmailMapper _mapper;

    public GetReceivedEmailQueryHandler(
        IReceivedEmailRepository repository,
        ReceivedEmailMapper mapper,
        ILogger<GetReceivedEmailQueryHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<ReceivedEmailResource?> Handle(GetReceivedEmailQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var email = await _repository.GetByIdAsync(request.Id);
            return email != null ? _mapper.ToResource(email) : null;
        }, nameof(GetReceivedEmailQuery), new { request.Id });
    }
}
