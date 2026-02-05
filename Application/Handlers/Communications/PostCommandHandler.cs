using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Exceptions;

namespace Klacks.Api.Application.Handlers.Communications;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<CommunicationResource>, CommunicationResource?>
{
    private readonly ICommunicationRepository _communicationRepository;
    private readonly AddressCommunicationMapper _addressCommunicationMapper;
    private readonly IUnitOfWork _unitOfWork;
    
    public PostCommandHandler(
        ICommunicationRepository communicationRepository,
        AddressCommunicationMapper addressCommunicationMapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _communicationRepository = communicationRepository;
        _addressCommunicationMapper = addressCommunicationMapper;
        _unitOfWork = unitOfWork;
        }

    public async Task<CommunicationResource?> Handle(PostCommand<CommunicationResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var communication = _addressCommunicationMapper.ToCommunicationEntity(request.Resource);
            await _communicationRepository.Add(communication);
            await _unitOfWork.CompleteAsync();
            return _addressCommunicationMapper.ToCommunicationResource(communication);
        }, 
        "creating communication", 
        new { ResourceId = request.Resource?.Id });
    }
}
