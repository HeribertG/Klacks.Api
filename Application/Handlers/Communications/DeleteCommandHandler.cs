using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;
using Klacks.Api.Domain.Exceptions;

namespace Klacks.Api.Application.Handlers.Communications;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<CommunicationResource>, CommunicationResource?>
{
    private readonly ICommunicationRepository _communicationRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    
    public DeleteCommandHandler(
        ICommunicationRepository communicationRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _communicationRepository = communicationRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        }

    public async Task<CommunicationResource?> Handle(DeleteCommand<CommunicationResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var existingCommunication = await _communicationRepository.Get(request.Id);
            if (existingCommunication == null)
            {
                throw new KeyNotFoundException($"Communication with ID {request.Id} not found.");
            }

            var communicationResource = _mapper.Map<CommunicationResource>(existingCommunication);
            await _communicationRepository.Delete(request.Id);
            await _unitOfWork.CompleteAsync();

            return communicationResource;
        }, 
        "deleting communication", 
        new { CommunicationId = request.Id });
    }
}
