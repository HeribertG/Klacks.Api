using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.Breaks;

public class PostCommandHandler : IRequestHandler<PostCommand<BreakResource>, BreakResource?>
{
    private readonly IBreakRepository _breakRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PostCommandHandler> _logger;

    public PostCommandHandler(
        IBreakRepository breakRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
    {
        _breakRepository = breakRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<BreakResource?> Handle(PostCommand<BreakResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var breakEntity = _mapper.Map<Break>(request.Resource);
            await _breakRepository.Add(breakEntity);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<BreakResource>(breakEntity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while adding a new break. ID: {BreakId}", request.Resource.Id);
            throw;
        }
    }
}
