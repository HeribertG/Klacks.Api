// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for updating a client. After the update is committed it raises one ContractChangedEvent
/// per contract whose client assignment actually changed (added, removed, re-dated, re-bound or
/// toggled), so persisted work surcharges of this client are recalculated from the earliest affected
/// assignment date. A request without client-contract data never triggers a recalculation.
///
/// Two write surfaces of the aggregate are gated separately, and deliberately not by the same rule.
/// Client-contract assignments (which contract a person holds, from when, until when) are supervisor
/// work: a caller holding CanEditContracts may change them, which is Admin and Authorised but not the
/// Planer floor (Permissions.PlannerFloor carries CanViewContracts only). Group memberships stay
/// Admin-only because they move the visibility boundary itself.
///
/// Stated honestly: over HTTP this contract check refuses nobody who can reach it. ClientsController.Put
/// already routes every caller without CanEditClients into the notes-only command, and every role that
/// holds CanEditClients holds CanEditContracts too. The check is the written decision and the barrier for
/// callers that reach the handler on another path; it starts separating HTTP callers the moment the two
/// rights stop travelling together.
/// </summary>
/// <param name="request">Contains the client resource with the new values</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Events;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Email;

namespace Klacks.Api.Application.Handlers.Clients;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<ClientResource>, ClientResource?>
{
    private const string ContractsDeniedMessage =
        "Changing client contract assignments requires the right to edit contracts.";

    private const string GroupsDeniedMessage = "Only administrators can modify client groups";

    private readonly IClientRepository _clientRepository;
    private readonly ClientMapper _clientMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGroupVisibilityService _groupVisibilityService;
    private readonly IEmailClientAssignmentService _emailAssignmentService;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly IUserService _userService;

    public PutCommandHandler(
        IClientRepository clientRepository,
        ClientMapper clientMapper,
        IUnitOfWork unitOfWork,
        IGroupVisibilityService groupVisibilityService,
        IEmailClientAssignmentService emailAssignmentService,
        IDomainEventDispatcher eventDispatcher,
        IUserService userService,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _clientRepository = clientRepository;
        _clientMapper = clientMapper;
        _unitOfWork = unitOfWork;
        _groupVisibilityService = groupVisibilityService;
        _emailAssignmentService = emailAssignmentService;
        _eventDispatcher = eventDispatcher;
        _userService = userService;
    }

    public async Task<ClientResource?> Handle(PutCommand<ClientResource> request, CancellationToken cancellationToken)
    {
        List<ContractChangedEvent> contractEvents = [];

        var result = await ExecuteAsync(async () =>
        {
            var existingClient = await _clientRepository.GetTrackedForUpdate(request.Resource.Id);
            if (existingClient == null)
            {
                throw new KeyNotFoundException($"Client with ID {request.Resource.Id} not found");
            }

            var isAdmin = await _groupVisibilityService.IsAdmin();
            if (!isAdmin)
            {
                if (HasClientContractsChanged(existingClient.ClientContracts, request.Resource.ClientContracts)
                    && !Permissions.HasPermission(_userService.GetRights(), Permissions.CanEditContracts))
                {
                    _logger.LogWarning(
                        "User without {Permission} attempted to modify ClientContracts for client {ClientId}",
                        Permissions.CanEditContracts,
                        request.Resource.Id);
                    throw new InvalidRequestException(ContractsDeniedMessage);
                }

                if (HasGroupItemsChanged(existingClient.GroupItems, request.Resource.GroupItems))
                {
                    _logger.LogWarning("Non-admin user attempted to modify GroupItems for client {ClientId}", request.Resource.Id);
                    throw new InvalidRequestException(GroupsDeniedMessage);
                }
            }

            var contractsBefore = SnapshotClientContracts(existingClient.ClientContracts);

            var client = _clientMapper.ToEntity(request.Resource);
            var updatedClient = await _clientRepository.Put(client, existingClient);
            if (updatedClient == null)
            {
                return null;
            }
            await _unitOfWork.CompleteAsync();

            contractEvents = BuildContractChangedEvents(request.Resource.Id, contractsBefore, request.Resource.ClientContracts);

            if (request.Resource.Communications.Any(c =>
                c.Type is CommunicationTypeEnum.PrivateMail or CommunicationTypeEnum.OfficeMail))
            {
                await _emailAssignmentService.AssignInboxEmailsToClientsAsync();
            }

            _logger.LogInformation("Client updated: {ClientId}", request.Resource.Id);

            return _clientMapper.ToResource(updatedClient);
        },
        "updating",
        new { });

        if (result != null)
        {
            await DispatchContractChangedEventsAsync(contractEvents);
        }

        return result;
    }

    private async Task DispatchContractChangedEventsAsync(List<ContractChangedEvent> contractEvents)
    {
        foreach (var contractEvent in contractEvents)
        {
            try
            {
                await _eventDispatcher.DispatchAsync(contractEvent, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Post-commit dispatch of {EventName} failed for contract {ContractId} of client {ClientId}; the client update is persisted and remains unaffected.",
                    nameof(ContractChangedEvent),
                    contractEvent.ContractId,
                    contractEvent.ClientId);
            }
        }
    }

    private static List<ClientContractSnapshot> SnapshotClientContracts(ICollection<Domain.Models.Staffs.ClientContract>? existing)
    {
        if (existing == null)
        {
            return [];
        }

        return existing
            .Where(cc => !cc.IsDeleted)
            .Select(cc => new ClientContractSnapshot(cc.Id, cc.ContractId, cc.FromDate, cc.UntilDate, cc.IsActive))
            .ToList();
    }

    private static List<ContractChangedEvent> BuildContractChangedEvents(
        Guid clientId,
        List<ClientContractSnapshot> before,
        ICollection<ClientContractResource>? incoming)
    {
        if (incoming == null)
        {
            return [];
        }

        var affectedFromByContract = new Dictionary<Guid, DateOnly>();

        void Touch(Guid contractId, DateOnly from)
        {
            if (!affectedFromByContract.TryGetValue(contractId, out var current) || from < current)
            {
                affectedFromByContract[contractId] = from;
            }
        }

        foreach (var incomingContract in incoming)
        {
            var existingContract = before.FirstOrDefault(c => c.Id == incomingContract.Id);
            if (existingContract == null)
            {
                Touch(incomingContract.ContractId, incomingContract.FromDate);
                continue;
            }

            if (existingContract.ContractId != incomingContract.ContractId ||
                existingContract.FromDate != incomingContract.FromDate ||
                existingContract.UntilDate != incomingContract.UntilDate ||
                existingContract.IsActive != incomingContract.IsActive)
            {
                Touch(existingContract.ContractId, existingContract.FromDate);
                Touch(incomingContract.ContractId, incomingContract.FromDate);
            }
        }

        var incomingIds = incoming.Select(c => c.Id).ToHashSet();
        foreach (var removedContract in before.Where(c => !incomingIds.Contains(c.Id)))
        {
            Touch(removedContract.ContractId, removedContract.FromDate);
        }

        return affectedFromByContract
            .Select(pair => new ContractChangedEvent(pair.Key, clientId, pair.Value, null))
            .ToList();
    }

    private bool HasClientContractsChanged(ICollection<Domain.Models.Staffs.ClientContract> existing, ICollection<ClientContractResource>? incoming)
    {
        if (incoming == null || !incoming.Any())
        {
            return existing != null && existing.Any();
        }

        if (existing == null || existing.Count != incoming.Count)
        {
            return true;
        }

        foreach (var incomingContract in incoming)
        {
            var existingContract = existing.FirstOrDefault(c => c.Id == incomingContract.Id);
            if (existingContract == null)
            {
                return true;
            }

            if (existingContract.ContractId != incomingContract.ContractId ||
                existingContract.IsActive != incomingContract.IsActive ||
                existingContract.FromDate != incomingContract.FromDate ||
                existingContract.UntilDate != incomingContract.UntilDate)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasGroupItemsChanged(ICollection<Domain.Models.Associations.GroupItem> existing, ICollection<ClientGroupItemResource>? incoming)
    {
        if (incoming == null || !incoming.Any())
        {
            return existing != null && existing.Any(gi => gi.ClientId.HasValue && !gi.ShiftId.HasValue);
        }

        var existingClientGroups = existing.Where(gi => gi.ClientId.HasValue && !gi.ShiftId.HasValue).ToList();
        if (existingClientGroups.Count != incoming.Count)
        {
            return true;
        }

        foreach (var incomingItem in incoming)
        {
            var existingItem = existingClientGroups.FirstOrDefault(g => g.GroupId == incomingItem.GroupId);
            if (existingItem == null)
            {
                return true;
            }

            if (existingItem.ValidFrom != incomingItem.ValidFrom ||
                existingItem.ValidUntil != incomingItem.ValidUntil)
            {
                return true;
            }
        }

        return false;
    }

    private sealed record ClientContractSnapshot(Guid Id, Guid ContractId, DateOnly FromDate, DateOnly? UntilDate, bool IsActive);
}
