using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.Communications;

public class PutCommandHandler : IRequestHandler<PutCommand<CommunicationResource>, CommunicationResource?>
{
    private readonly ICommunicationRepository _communicationRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PutCommandHandler> _logger;

    public PutCommandHandler(
        ICommunicationRepository communicationRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
    {
        _communicationRepository = communicationRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CommunicationResource?> Handle(PutCommand<CommunicationResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var existingCommunication = await _communicationRepository.Get(request.Resource.Id);
            if (existingCommunication == null)
            {
                _logger.LogWarning("Communication with ID {CommunicationId} not found.", request.Resource.Id);
                return null;
            }

            _mapper.Map(request.Resource, existingCommunication);
            await _communicationRepository.Put(existingCommunication);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<CommunicationResource>(existingCommunication);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating communication with ID {CommunicationId}.", request.Resource.Id);
            throw;
        }
    }
}
