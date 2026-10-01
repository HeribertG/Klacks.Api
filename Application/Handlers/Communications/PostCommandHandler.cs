// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Creates one client communication entry; a new mail address triggers the inbox-to-client assignment.
/// An entry for a client outside the caller's group visibility is refused exactly like an entry for a
/// client that does not exist: nothing is written and no mail is assigned.
/// </summary>
/// <param name="request">Carries the communication entry, including the id of the client it belongs to</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Communications;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<CommunicationResource>, CommunicationResource?>
{
    private readonly ICommunicationRepository _communicationRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly AddressCommunicationMapper _addressCommunicationMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailClientAssignmentService _emailAssignmentService;

    public PostCommandHandler(
        ICommunicationRepository communicationRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        AddressCommunicationMapper addressCommunicationMapper,
        IUnitOfWork unitOfWork,
        IEmailClientAssignmentService emailAssignmentService,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _communicationRepository = communicationRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _addressCommunicationMapper = addressCommunicationMapper;
        _unitOfWork = unitOfWork;
        _emailAssignmentService = emailAssignmentService;
    }

    public async Task<CommunicationResource?> Handle(PostCommand<CommunicationResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            if (!await _clientVisibilityGuard.IsVisibleAsync(request.Resource.ClientId, cancellationToken))
            {
                throw new KeyNotFoundException($"Client with ID {request.Resource.ClientId} not found");
            }

            var communication = _addressCommunicationMapper.ToCommunicationEntity(request.Resource);
            await _communicationRepository.Add(communication);
            await _unitOfWork.CompleteAsync();

            if (request.Resource.Type is CommunicationTypeEnum.PrivateMail or CommunicationTypeEnum.OfficeMail)
            {
                await _emailAssignmentService.AssignInboxEmailsToClientsAsync();
            }

            return _addressCommunicationMapper.ToCommunicationResource(communication);
        },
        "creating communication",
        new { ResourceId = request.Resource?.Id });
    }
}
