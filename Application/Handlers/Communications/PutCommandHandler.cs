// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
    private readonly AddressCommunicationMapper _addressCommunicationMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailClientAssignmentService _emailAssignmentService;

    public PutCommandHandler(
        ICommunicationRepository communicationRepository,
        AddressCommunicationMapper addressCommunicationMapper,
        IUnitOfWork unitOfWork,
        IEmailClientAssignmentService emailAssignmentService,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _communicationRepository = communicationRepository;
        _addressCommunicationMapper = addressCommunicationMapper;
        _unitOfWork = unitOfWork;
        _emailAssignmentService = emailAssignmentService;
    }

    public async Task<CommunicationResource?> Handle(PutCommand<CommunicationResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var existingCommunication = await _communicationRepository.Get(request.Resource.Id);
            if (existingCommunication == null)
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
