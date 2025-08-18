using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Application.Handlers.Clients;

public class PutCommandHandler : IRequestHandler<PutCommand<ClientResource>, ClientResource?>
{
    private readonly IClientRepository _clientRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PutCommandHandler> _logger;

    public PutCommandHandler(
        IClientRepository clientRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
    {
        _clientRepository = clientRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ClientResource?> Handle(PutCommand<ClientResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var client = _mapper.Map<Klacks.Api.Domain.Models.Staffs.Client>(request.Resource);
            var updatedClient = await _clientRepository.Put(client);
            await _unitOfWork.CompleteAsync();
            _logger.LogInformation("Employee with ID {ClientId} updated successfully.", request.Resource.Id);
            return _mapper.Map<ClientResource>(updatedClient);
        }
        catch (KeyNotFoundException)
        {
            _logger.LogWarning("Employee with ID {ClientId} not found.", request.Resource.Id);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating client with ID {ClientId}.", request.Resource.Id);
            throw;
        }
    }
}
