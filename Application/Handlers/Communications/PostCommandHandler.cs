// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
    private readonly AddressCommunicationMapper _addressCommunicationMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailClientAssignmentService _emailAssignmentService;

    public PostCommandHandler(
        ICommunicationRepository communicationRepository,
        AddressCommunicationMapper addressCommunicationMapper,
        IUnitOfWork unitOfWork,
        IEmailClientAssignmentService emailAssignmentService,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _communicationRepository = communicationRepository;
        _addressCommunicationMapper = addressCommunicationMapper;
        _unitOfWork = unitOfWork;
        _emailAssignmentService = emailAssignmentService;
    }

    public async Task<CommunicationResource?> Handle(PostCommand<CommunicationResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
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
