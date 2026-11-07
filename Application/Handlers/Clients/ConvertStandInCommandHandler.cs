// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Turns a stand-in into an employee or an external employee in place (ClientTypeRules.CanConvert): the same client gets
/// the target type, first name, name and gender and, when given, an address and communications; its id, works,
/// membership, group items and qualifications stay as they are. From then on the client is no stand-in, so the period
/// close lock and the payroll gate no longer see its works, and its target hours come from its contract or the
/// contractless defaults instead of 0. Because surcharge and absence macros read those target hours, the committed
/// conversion queues the same thorough recalculation of the client's real plan as a contract change (from the start of
/// its membership to its latest work, only when it has works there); the cached period hours are recalculated by it.
/// A client outside the caller's visibility is answered like a missing one; a client that is no stand-in is refused.
/// Two concurrent conversions of the same stand-in both succeed, the last one's person data stays (no concurrency token
/// on Client); the Idempotency-Key of the endpoint keeps an automatic retry from converting twice.
/// </summary>
/// <param name="clientRepository">Loads the stand-in as a tracked aggregate</param>
/// <param name="clientVisibilityGuard">Checks that the caller sees the client</param>
/// <param name="clientMapper">Maps the address, the communications and the result</param>
/// <param name="unitOfWork">Saves the conversion</param>
/// <param name="emailAssignmentService">Assigns inbox e-mails once the person has an e-mail address</param>
/// <param name="recalculationScope">Finds the latest work date of the client</param>
/// <param name="recalculationQueue">Takes the thorough recalculation of the client's works</param>

using Klacks.Api.Application.Commands.Clients;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Clients;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Clients;

public class ConvertStandInCommandHandler : BaseHandler, IRequestHandler<ConvertStandInCommand, ClientResource>
{
    private const string ClientNotFoundMessage = "Client with ID {0} not found";
    private const string NotConvertibleMessage = "Only a stand-in can be converted, and only into an employee or an external employee.";

    private readonly IClientRepository _clientRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly ClientMapper _clientMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailClientAssignmentService _emailAssignmentService;
    private readonly ISurchargeRecalculationScope _recalculationScope;
    private readonly IThoroughRecalculationQueue _recalculationQueue;

    public ConvertStandInCommandHandler(
        IClientRepository clientRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        ClientMapper clientMapper,
        IUnitOfWork unitOfWork,
        IEmailClientAssignmentService emailAssignmentService,
        ISurchargeRecalculationScope recalculationScope,
        IThoroughRecalculationQueue recalculationQueue,
        ILogger<ConvertStandInCommandHandler> logger)
        : base(logger)
    {
        _clientRepository = clientRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _clientMapper = clientMapper;
        _unitOfWork = unitOfWork;
        _emailAssignmentService = emailAssignmentService;
        _recalculationScope = recalculationScope;
        _recalculationQueue = recalculationQueue;
    }

    public async Task<ClientResource> Handle(ConvertStandInCommand request, CancellationToken cancellationToken)
    {
        var client = await ExecuteAsync(async () =>
        {
            var standIn = await _clientVisibilityGuard.IsVisibleAsync(request.ClientId, cancellationToken)
                ? await _clientRepository.GetTrackedForUpdate(request.ClientId)
                : null;
            if (standIn == null)
            {
                throw new KeyNotFoundException(string.Format(ClientNotFoundMessage, request.ClientId));
            }

            var targetType = (EntityTypeEnum)request.Resource.TargetType;
            if (!ClientTypeRules.CanConvert(standIn.Type, targetType))
            {
                throw new InvalidRequestException(NotConvertibleMessage);
            }

            ApplyPersonData(standIn, targetType, request.Resource);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Stand-in converted: {ClientId} to {Type}", standIn.Id, targetType);

            return standIn;
        },
        "converting stand-in",
        new { request.ClientId, request.Resource.TargetType });

        if (client.Communications.Any(c => c.Type is CommunicationTypeEnum.PrivateMail or CommunicationTypeEnum.OfficeMail))
        {
            await _emailAssignmentService.AssignInboxEmailsToClientsAsync();
        }

        await QueueRecalculationAsync(client);

        return _clientMapper.ToResource(client);
    }

    private void ApplyPersonData(Client client, EntityTypeEnum targetType, ConvertStandInResource resource)
    {
        client.Type = targetType;
        client.FirstName = resource.FirstName.Trim();
        client.Name = resource.Name.Trim();
        client.Gender = resource.Gender;

        if (resource.Address != null)
        {
            var address = _clientMapper.ToAddressEntity(resource.Address);
            address.Id = Guid.Empty;
            address.ClientId = client.Id;
            client.Addresses.Add(address);
        }

        foreach (var communicationResource in resource.Communications ?? [])
        {
            var communication = _clientMapper.ToCommunicationEntity(communicationResource);
            communication.Id = Guid.Empty;
            communication.ClientId = client.Id;
            client.Communications.Add(communication);
        }
    }

    private async Task QueueRecalculationAsync(Client client)
    {
        if (client.Membership == null)
        {
            return;
        }

        try
        {
            var from = DateOnly.FromDateTime(client.Membership.ValidFrom);
            var until = await _recalculationScope.GetLatestWorkDateAsync([client.Id], from, CancellationToken.None);
            if (until is null || until < from)
            {
                return;
            }

            if (!_recalculationQueue.QueueRecalculation(from, until.Value, null, null, [client.Id]))
            {
                _logger.LogWarning(
                    "Recalculation queue full: the converted stand-in {ClientId} keeps its surcharges until the next recalculation",
                    client.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to queue the recalculation of converted stand-in {ClientId}; the conversion is committed and remains unaffected",
                client.Id);
        }
    }
}
