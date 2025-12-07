using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Exceptions;

namespace Klacks.Api.Application.Handlers.BreakPlaceholders;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<BreakResource>, BreakResource?>
{
    private readonly IBreakPlaceholderRepository _breakPlaceholderRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCommandHandler(
        IBreakPlaceholderRepository breakPlaceholderRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _breakPlaceholderRepository = breakPlaceholderRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        }

    public async Task<BreakResource?> Handle(DeleteCommand<BreakResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var existingBreak = await _breakPlaceholderRepository.Get(request.Id);
            if (existingBreak == null)
            {
                throw new KeyNotFoundException($"Break with ID {request.Id} not found.");
            }

            var breakResource = _scheduleMapper.ToBreakResource(existingBreak);
            await _breakPlaceholderRepository.Delete(request.Id);
            await _unitOfWork.CompleteAsync();

            return breakResource;
        },
        "deleting break",
        new { BreakId = request.Id });
    }
}
