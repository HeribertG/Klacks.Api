// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Soft-deletes a client and reassigns its mails. A stand-in that still holds works in the real plan or in an open
/// scenario is refused: its services would stay planned on a client nobody sees, the period close would still count
/// them, and accepting the scenario would write its works into the real plan on the deleted stand-in.
/// </summary>
/// <param name="request">Carries the id of the client to delete</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Domain.Services.Clients;

namespace Klacks.Api.Application.Handlers.Clients;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<ClientResource>, ClientResource?>
{
    private readonly IClientRepository _clientRepository;
    private readonly IWorkRepository _workRepository;
    private readonly IEmailClientAssignmentService _emailAssignmentService;
    private readonly ClientMapper _clientMapper;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCommandHandler(
        IClientRepository clientRepository,
        IWorkRepository workRepository,
        IEmailClientAssignmentService emailAssignmentService,
        ClientMapper clientMapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _clientRepository = clientRepository;
        _workRepository = workRepository;
        _emailAssignmentService = emailAssignmentService;
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

            if (ClientTypeRules.IsStandIn(client.Type))
            {
                var workCount = await _workRepository.CountWorksOfClientAsync(client.Id, cancellationToken);
                if (workCount > 0)
                {
                    throw new StandInHasWorksException(client.Name, workCount);
                }
            }

            var clientResource = _clientMapper.ToResource(client);
            await _clientRepository.Delete(request.Id);
            await _unitOfWork.CompleteAsync();

            await _emailAssignmentService.ReassignOrphanedEmailsAsync();

            return clientResource;
        },
        "deleting client",
        new { ClientId = request.Id });
    }
}
