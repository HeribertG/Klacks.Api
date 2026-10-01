// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for creating a new client including its addresses, communications, image and group memberships.
/// Every group the new client is placed in must be visible to the caller; a hidden group is answered exactly
/// like a missing one, so a non-admin cannot create a client inside a group outside their visibility.
/// </summary>
/// <param name="request">Contains the client resource to create</param>
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
    private const string GroupNotFoundMessage = "Group with ID {0} not found";

    private readonly IClientRepository _clientRepository;
    private readonly ClientMapper _clientMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailClientAssignmentService _emailAssignmentService;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;

    public PostCommandHandler(
        IClientRepository clientRepository,
        ClientMapper clientMapper,
        IUnitOfWork unitOfWork,
        IEmailClientAssignmentService emailAssignmentService,
        IGroupVisibilityGuard groupVisibilityGuard,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _clientRepository = clientRepository;
        _clientMapper = clientMapper;
        _unitOfWork = unitOfWork;
        _emailAssignmentService = emailAssignmentService;
        _groupVisibilityGuard = groupVisibilityGuard;
    }

    public async Task<ClientResource?> Handle(PostCommand<ClientResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            await EnsureGroupsVisibleAsync(request.Resource, cancellationToken);

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

    private async Task EnsureGroupsVisibleAsync(ClientResource resource, CancellationToken cancellationToken)
    {
        var groupIds = resource.GroupItems?.Select(g => g.GroupId).Distinct().ToList() ?? [];
        if (await _groupVisibilityGuard.AreAllGroupsVisibleAsync(groupIds, cancellationToken))
        {
            return;
        }

        foreach (var groupId in groupIds)
        {
            if (!await _groupVisibilityGuard.IsGroupVisibleAsync(groupId, cancellationToken))
            {
                throw new KeyNotFoundException(string.Format(GroupNotFoundMessage, groupId));
            }
        }

        throw new KeyNotFoundException(string.Format(GroupNotFoundMessage, groupIds[0]));
    }
}
