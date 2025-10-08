using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Application.Handlers.Clients;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<ClientResource>, ClientResource?>
{
    private readonly IClientRepository _clientRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    
    public PostCommandHandler(
        IClientRepository clientRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _clientRepository = clientRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        }

    public async Task<ClientResource?> Handle(PostCommand<ClientResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            _logger.LogInformation("🔍 [BACKEND CREATE] Received ClientResource with {ContractCount} contracts",
                request.Resource.ClientContracts?.Count ?? 0);

            foreach (var contract in request.Resource.ClientContracts ?? new List<ClientContractResource>())
            {
                _logger.LogInformation("🔍 [BACKEND CREATE] Contract: ClientId={ClientId}, ContractId={ContractId}, IsActive={IsActive}, FromDate={FromDate}, UntilDate={UntilDate}",
                    contract.ClientId, contract.ContractId, contract.IsActive, contract.FromDate, contract.UntilDate);
            }

            var client = _mapper.Map<Domain.Models.Staffs.Client>(request.Resource);

            _logger.LogInformation("🔍 [BACKEND CREATE] Mapped Domain Client with {ContractCount} contracts",
                client.ClientContracts?.Count ?? 0);

            await _clientRepository.Add(client);
            await _unitOfWork.CompleteAsync();

            var result = _mapper.Map<ClientResource>(client);

            _logger.LogInformation("🔍 [BACKEND CREATE] Final ClientResource has {ContractCount} contracts",
                result.ClientContracts?.Count ?? 0);

            return result;
        },
        "creating client",
        new { ClientId = request.Resource?.Id });
    }
}
