using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Infrastructure.Interfaces;
using Klacks.Api.Presentation.DTOs.Associations;
using Klacks.Api.Presentation.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Clients;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<ClientResource>, ClientResource?>
{
    private readonly IClientRepository _clientRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGroupVisibilityService _groupVisibilityService;

    public PutCommandHandler(
        IClientRepository clientRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        IGroupVisibilityService groupVisibilityService,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _clientRepository = clientRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _groupVisibilityService = groupVisibilityService;
        }

    public async Task<ClientResource?> Handle(PutCommand<ClientResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            _logger.LogInformation("🔍 [BACKEND SAVE] Received ClientResource with {ContractCount} contracts",
                request.Resource.ClientContracts?.Count ?? 0);

            foreach (var contract in request.Resource.ClientContracts ?? new List<ClientContractResource>())
            {
                _logger.LogInformation("🔍 [BACKEND SAVE] Contract: ClientId={ClientId}, ContractId={ContractId}, IsActive={IsActive}, FromDate={FromDate}, UntilDate={UntilDate}",
                    contract.ClientId, contract.ContractId, contract.IsActive, contract.FromDate, contract.UntilDate);
            }

            var isAdmin = await _groupVisibilityService.IsAdmin();
            if (!isAdmin)
            {
                var existingClient = await _clientRepository.Get(request.Resource.Id);
                if (existingClient == null)
                {
                    throw new KeyNotFoundException($"Client with ID {request.Resource.Id} not found");
                }

                if (HasClientContractsChanged(existingClient.ClientContracts, request.Resource.ClientContracts))
                {
                    _logger.LogWarning("Non-admin user attempted to modify ClientContracts for client {ClientId}", request.Resource.Id);
                    throw new InvalidRequestException("Only administrators can modify client contracts");
                }

                if (HasGroupItemsChanged(existingClient.GroupItems, request.Resource.GroupItems))
                {
                    _logger.LogWarning("Non-admin user attempted to modify GroupItems for client {ClientId}", request.Resource.Id);
                    throw new InvalidRequestException("Only administrators can modify client groups");
                }
            }

            var client = _mapper.Map<Domain.Models.Staffs.Client>(request.Resource);

            _logger.LogInformation("🔍 [BACKEND SAVE] Mapped Domain Client with {ContractCount} contracts",
                client.ClientContracts?.Count ?? 0);

            var updatedClient = await _clientRepository.Put(client);

            _logger.LogInformation("🔍 [BACKEND SAVE] Updated Client from DB with {ContractCount} contracts",
                updatedClient?.ClientContracts?.Count ?? 0);

            await _unitOfWork.CompleteAsync();

            var result = _mapper.Map<ClientResource>(updatedClient);

            _logger.LogInformation("🔍 [BACKEND SAVE] Final ClientResource has {ContractCount} contracts",
                result.ClientContracts?.Count ?? 0);

            return result;
        },
        "updating",
        new { });
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
}
