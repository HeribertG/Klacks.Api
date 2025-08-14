using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Application.Handlers.Clients;

public class DeleteCommandHandler : IRequestHandler<DeleteCommand<ClientResource>, ClientResource?>
{
    private readonly IClientRepository _clientRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteCommandHandler> _logger;

    public DeleteCommandHandler(
        IClientRepository clientRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
    {
        _clientRepository = clientRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ClientResource?> Handle(DeleteCommand<ClientResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var client = await _clientRepository.Get(request.Id);
            if (client == null)
            {
                _logger.LogWarning("Client with ID {ClientId} not found for deletion.", request.Id);
                return null;
            }

            var clientResource = _mapper.Map<ClientResource>(client);
            await _clientRepository.Delete(request.Id);
            await _unitOfWork.CompleteAsync();
            _logger.LogInformation("Client with ID {ClientId} deleted successfully.", request.Id);
            return clientResource;
        }
        catch (KeyNotFoundException)
        {
            _logger.LogWarning("Client with ID {ClientId} not found for deletion.", request.Id);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting client with ID {ClientId}.", request.Id);
            throw;
        }
    }
}
