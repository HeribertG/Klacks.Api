using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;

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
            _logger.LogInformation("🔍 [BACKEND CREATE] Received ClientResource with {ContractCount} contracts, HasImage={HasImage}",
                request.Resource.ClientContracts?.Count ?? 0, request.Resource.ClientImage != null);

            if (request.Resource.ClientImage != null)
            {
                _logger.LogInformation("🔍 [BACKEND CREATE] ClientImage: Id={Id}, ClientId={ClientId}, ContentType={ContentType}, FileName={FileName}, FileSize={FileSize}, ImageDataLength={ImageDataLength}",
                    request.Resource.ClientImage.Id, request.Resource.ClientImage.ClientId,
                    request.Resource.ClientImage.ContentType, request.Resource.ClientImage.FileName,
                    request.Resource.ClientImage.FileSize, request.Resource.ClientImage.ImageData?.Length ?? 0);

                if (request.Resource.ClientImage.ClientId == Guid.Empty)
                {
                    _logger.LogInformation("🔍 [BACKEND CREATE] ClientImage.ClientId is empty, will be set after client creation");
                }
            }

            foreach (var contract in request.Resource.ClientContracts ?? new List<ClientContractResource>())
            {
                _logger.LogInformation("🔍 [BACKEND CREATE] Contract: ClientId={ClientId}, ContractId={ContractId}, IsActive={IsActive}, FromDate={FromDate}, UntilDate={UntilDate}",
                    contract.ClientId, contract.ContractId, contract.IsActive, contract.FromDate, contract.UntilDate);
            }

            var client = _mapper.Map<Domain.Models.Staffs.Client>(request.Resource);

            if (client.ClientImage != null)
            {
                _logger.LogInformation("🔍 [BACKEND CREATE] Setting ClientImage.ClientId to Client.Id");
                client.ClientImage.ClientId = client.Id;
            }

            _logger.LogInformation("🔍 [BACKEND CREATE] Mapped Domain Client with {ContractCount} contracts, HasImage={HasImage}",
                client.ClientContracts?.Count ?? 0, client.ClientImage != null);

            if (client.ClientImage != null)
            {
                _logger.LogInformation("🔍 [BACKEND CREATE] Mapped ClientImage: Id={Id}, ClientId={ClientId}, ContentType={ContentType}, ImageDataLength={ImageDataLength}",
                    client.ClientImage.Id, client.ClientImage.ClientId,
                    client.ClientImage.ContentType, client.ClientImage.ImageData?.Length ?? 0);
            }

            await _clientRepository.Add(client);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("🔍 [BACKEND CREATE] After save - Client.Id={ClientId}, HasImage={HasImage}",
                client.Id, client.ClientImage != null);

            if (client.ClientImage != null)
            {
                _logger.LogInformation("🔍 [BACKEND CREATE] After save - ClientImage.Id={Id}, ClientId={ClientId}",
                    client.ClientImage.Id, client.ClientImage.ClientId);
            }

            var result = _mapper.Map<ClientResource>(client);

            _logger.LogInformation("🔍 [BACKEND CREATE] Final ClientResource has {ContractCount} contracts",
                result.ClientContracts?.Count ?? 0);

            return result;
        },
        "creating client",
        new { ClientId = request.Resource?.Id });
    }
}
