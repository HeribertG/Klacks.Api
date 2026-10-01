// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Deletes one client communication entry and afterwards reassigns inbox mails that lost their client.
/// An entry owned by a client outside the caller's group visibility is refused exactly like a missing
/// entry: nothing is deleted and no mail is reassigned.
/// </summary>
/// <param name="request">Carries the id of the communication entry to delete</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Email;

namespace Klacks.Api.Application.Handlers.Communications;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<CommunicationResource>, CommunicationResource?>
{
    private readonly ICommunicationRepository _communicationRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly IEmailClientAssignmentService _emailAssignmentService;
    private readonly AddressCommunicationMapper _addressCommunicationMapper;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCommandHandler(
        ICommunicationRepository communicationRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        IEmailClientAssignmentService emailAssignmentService,
        AddressCommunicationMapper addressCommunicationMapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _communicationRepository = communicationRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _emailAssignmentService = emailAssignmentService;
        _addressCommunicationMapper = addressCommunicationMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<CommunicationResource?> Handle(DeleteCommand<CommunicationResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var existingCommunication = await _communicationRepository.Get(request.Id);
            if (existingCommunication == null
                || !await _clientVisibilityGuard.IsVisibleAsync(existingCommunication.ClientId, cancellationToken))
            {
                throw new KeyNotFoundException($"Communication with ID {request.Id} not found.");
            }

            var communicationResource = _addressCommunicationMapper.ToCommunicationResource(existingCommunication);
            await _communicationRepository.Delete(request.Id);
            await _unitOfWork.CompleteAsync();

            await _emailAssignmentService.ReassignOrphanedEmailsAsync();

            return communicationResource;
        },
        "deleting communication",
        new { CommunicationId = request.Id });
    }
}
