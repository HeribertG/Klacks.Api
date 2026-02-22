using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.Communications;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<CommunicationResource>, CommunicationResource?>
{
    private readonly ICommunicationRepository _communicationRepository;
    private readonly AddressCommunicationMapper _addressCommunicationMapper;
    private readonly IUnitOfWork _unitOfWork;
    
    public PutCommandHandler(
        ICommunicationRepository communicationRepository,
        AddressCommunicationMapper addressCommunicationMapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _communicationRepository = communicationRepository;
        _addressCommunicationMapper = addressCommunicationMapper;
        _unitOfWork = unitOfWork;
        }

    public async Task<CommunicationResource?> Handle(PutCommand<CommunicationResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var existingCommunication = await _communicationRepository.Get(request.Resource.Id);
            if (existingCommunication == null)
            {
                throw new KeyNotFoundException($"Communication with ID {request.Resource.Id} not found.");
            }

            var updatedCommunication = _addressCommunicationMapper.ToCommunicationEntity(request.Resource);
            updatedCommunication.CreateTime = existingCommunication.CreateTime;
            updatedCommunication.CurrentUserCreated = existingCommunication.CurrentUserCreated;
            existingCommunication = updatedCommunication;
            await _communicationRepository.Put(existingCommunication);
            await _unitOfWork.CompleteAsync();
            return _addressCommunicationMapper.ToCommunicationResource(existingCommunication);
        }, 
        "updating communication", 
        new { CommunicationId = request.Resource.Id });
    }
}
