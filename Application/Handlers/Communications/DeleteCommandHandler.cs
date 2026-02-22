using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.Communications;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<CommunicationResource>, CommunicationResource?>
{
    private readonly ICommunicationRepository _communicationRepository;
    private readonly AddressCommunicationMapper _addressCommunicationMapper;
    private readonly IUnitOfWork _unitOfWork;
    
    public DeleteCommandHandler(
        ICommunicationRepository communicationRepository,
        AddressCommunicationMapper addressCommunicationMapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _communicationRepository = communicationRepository;
        _addressCommunicationMapper = addressCommunicationMapper;
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

            var communicationResource = _addressCommunicationMapper.ToCommunicationResource(existingCommunication);
            await _communicationRepository.Delete(request.Id);
            await _unitOfWork.CompleteAsync();

            return communicationResource;
        }, 
        "deleting communication", 
        new { CommunicationId = request.Id });
    }
}
