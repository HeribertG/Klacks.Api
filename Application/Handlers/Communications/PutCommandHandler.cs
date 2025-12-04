using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Exceptions;

namespace Klacks.Api.Application.Handlers.Communications;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<CommunicationResource>, CommunicationResource?>
{
    private readonly ICommunicationRepository _communicationRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    
    public PutCommandHandler(
        ICommunicationRepository communicationRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _communicationRepository = communicationRepository;
        _mapper = mapper;
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

            _mapper.Map(request.Resource, existingCommunication);
            await _communicationRepository.Put(existingCommunication);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<CommunicationResource>(existingCommunication);
        }, 
        "updating communication", 
        new { CommunicationId = request.Resource.Id });
    }
}
