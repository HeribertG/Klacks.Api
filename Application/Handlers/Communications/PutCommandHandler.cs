// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Replaces one client communication entry with the sent values; a mail address triggers the
/// inbox-to-client assignment. The entry is rebuilt from the resource including its client id, so both
/// the stored owner and the incoming owner must be inside the caller's group visibility, otherwise the
/// request is refused exactly like a missing entry: nothing is written and no mail is assigned.
/// </summary>
/// <param name="request">Carries the communication entry with its new values, including the owning client id</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Communications;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<CommunicationResource>, CommunicationResource?>
{
    private readonly ICommunicationRepository _communicationRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly AddressCommunicationMapper _addressCommunicationMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailClientAssignmentService _emailAssignmentService;

    public PutCommandHandler(
        ICommunicationRepository communicationRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        AddressCommunicationMapper addressCommunicationMapper,
        IUnitOfWork unitOfWork,
        IEmailClientAssignmentService emailAssignmentService,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _communicationRepository = communicationRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _addressCommunicationMapper = addressCommunicationMapper;
        _unitOfWork = unitOfWork;
        _emailAssignmentService = emailAssignmentService;
    }

    public async Task<CommunicationResource?> Handle(PutCommand<CommunicationResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var existingCommunication = await _communicationRepository.GetNoTracking(request.Resource.Id);
            if (existingCommunication == null
                || !await _clientVisibilityGuard.AreAllVisibleAsync(
                    [existingCommunication.ClientId, request.Resource.ClientId], cancellationToken))
            {
                throw new KeyNotFoundException($"Communication with ID {request.Resource.Id} not found.");
            }

            var updatedCommunication = _addressCommunicationMapper.ToCommunicationEntity(request.Resource);
            updatedCommunication.CreateTime = existingCommunication.CreateTime;
            updatedCommunication.CurrentUserCreated = existingCommunication.CurrentUserCreated;
            existingCommunication = updatedCommunication;
            await _communicationRepository.Put(existingCommunication);
            await _unitOfWork.CompleteAsync();

            if (request.Resource.Type is CommunicationTypeEnum.PrivateMail or CommunicationTypeEnum.OfficeMail)
            {
                await _emailAssignmentService.AssignInboxEmailsToClientsAsync();
            }

            return _addressCommunicationMapper.ToCommunicationResource(existingCommunication);
        },
        "updating communication",
        new { CommunicationId = request.Resource.Id });
    }
}
