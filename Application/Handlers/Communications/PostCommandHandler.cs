using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;
using Klacks.Api.Domain.Exceptions;

namespace Klacks.Api.Application.Handlers.Communications;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<CommunicationResource>, CommunicationResource?>
{
    private readonly ICommunicationRepository _communicationRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    
    public PostCommandHandler(
        ICommunicationRepository communicationRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _communicationRepository = communicationRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        }

    public async Task<CommunicationResource?> Handle(PostCommand<CommunicationResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var communication = _mapper.Map<Communication>(request.Resource);
            await _communicationRepository.Add(communication);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<CommunicationResource>(communication);
        }, 
        "creating communication", 
        new { ResourceId = request.Resource?.Id });
    }
}
