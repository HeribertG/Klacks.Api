// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Email;

namespace Klacks.Api.Application.Handlers.Clients;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<ClientResource>, ClientResource?>
{
    private readonly IClientRepository _clientRepository;
    private readonly ClientMapper _clientMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailClientAssignmentService _emailAssignmentService;

    public PostCommandHandler(
        IClientRepository clientRepository,
        ClientMapper clientMapper,
        IUnitOfWork unitOfWork,
        IEmailClientAssignmentService emailAssignmentService,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _clientRepository = clientRepository;
        _clientMapper = clientMapper;
        _unitOfWork = unitOfWork;
        _emailAssignmentService = emailAssignmentService;
    }

    public async Task<ClientResource?> Handle(PostCommand<ClientResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var client = _clientMapper.ToEntity(request.Resource);

            if (client.ClientImage != null)
            {
                client.ClientImage.ClientId = client.Id;
            }

            await _clientRepository.Add(client);
            await _unitOfWork.CompleteAsync();

            if (request.Resource.Communications.Any(c =>
                c.Type is CommunicationTypeEnum.PrivateMail or CommunicationTypeEnum.OfficeMail))
            {
                await _emailAssignmentService.AssignInboxEmailsToClientsAsync();
            }

            _logger.LogInformation("Client created: {ClientId}", client.Id);

            return _clientMapper.ToResource(client);
        },
        "creating client",
        new { ClientId = request.Resource?.Id });
    }
}
