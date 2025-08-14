using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.Communications;

public class DeleteCommandHandler : IRequestHandler<DeleteCommand<CommunicationResource>, CommunicationResource?>
{
    private readonly ICommunicationRepository _communicationRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteCommandHandler> _logger;

    public DeleteCommandHandler(
        ICommunicationRepository communicationRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
    {
        _communicationRepository = communicationRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CommunicationResource?> Handle(DeleteCommand<CommunicationResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var existingCommunication = await _communicationRepository.Get(request.Id);
            if (existingCommunication == null)
            {
                _logger.LogWarning("Communication with ID {CommunicationId} not found for deletion.", request.Id);
                return null;
            }

            var communicationResource = _mapper.Map<CommunicationResource>(existingCommunication);
            await _communicationRepository.Delete(request.Id);
            await _unitOfWork.CompleteAsync();

            return communicationResource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting communication with ID {CommunicationId}.", request.Id);
            throw;
        }
    }
}
