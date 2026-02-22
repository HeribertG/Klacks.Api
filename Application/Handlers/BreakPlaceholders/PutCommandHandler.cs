using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.BreakPlaceholders;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<BreakPlaceholderResource>, BreakPlaceholderResource?>
{
    private readonly IBreakPlaceholderRepository _breakPlaceholderRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;

    public PutCommandHandler(
        IBreakPlaceholderRepository breakPlaceholderRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _breakPlaceholderRepository = breakPlaceholderRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        }

    public async Task<BreakPlaceholderResource?> Handle(PutCommand<BreakPlaceholderResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var existingBreak = await _breakPlaceholderRepository.Get(request.Resource.Id);
            if (existingBreak == null)
            {
                throw new KeyNotFoundException($"Break with ID {request.Resource.Id} not found.");
            }

            _scheduleMapper.UpdateBreakEntity(request.Resource, existingBreak);
            await _unitOfWork.CompleteAsync();
            return _scheduleMapper.ToBreakPlaceholderResource(existingBreak);
        },
        "updating break",
        new { BreakPlaceholderId = request.Resource.Id });
    }
}
