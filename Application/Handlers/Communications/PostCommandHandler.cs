using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.Communications;

public class PostCommandHandler : IRequestHandler<PostCommand<CommunicationResource>, CommunicationResource?>
{
    private readonly ICommunicationRepository _communicationRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PostCommandHandler> _logger;

    public PostCommandHandler(
        ICommunicationRepository communicationRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
    {
        _communicationRepository = communicationRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CommunicationResource?> Handle(PostCommand<CommunicationResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var communication = _mapper.Map<Communication>(request.Resource);
            await _communicationRepository.Add(communication);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<CommunicationResource>(communication);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while adding a new communication.");
            throw;
        }
    }
}
