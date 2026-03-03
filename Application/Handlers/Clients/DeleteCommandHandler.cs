// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.Clients;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<ClientResource>, ClientResource?>
{
    private readonly IClientRepository _clientRepository;
    private readonly ClientMapper _clientMapper;
    private readonly IUnitOfWork _unitOfWork;
    
    public DeleteCommandHandler(
        IClientRepository clientRepository,
        ClientMapper clientMapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _clientRepository = clientRepository;
        _clientMapper = clientMapper;
        _unitOfWork = unitOfWork;
        }

    public async Task<ClientResource?> Handle(DeleteCommand<ClientResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var client = await _clientRepository.Get(request.Id);
            if (client == null)
            {
                throw new KeyNotFoundException($"Client with ID {request.Id} not found.");
            }

            var clientResource = _clientMapper.ToResource(client);
            await _clientRepository.Delete(request.Id);
            await _unitOfWork.CompleteAsync();
            return clientResource;
        }, 
        "deleting client", 
        new { ClientId = request.Id });
    }
}
