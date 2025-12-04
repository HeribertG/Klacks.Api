using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Exceptions;

namespace Klacks.Api.Application.Handlers.Breaks;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<BreakResource>, BreakResource?>
{
    private readonly IBreakRepository _breakRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    
    public PutCommandHandler(
        IBreakRepository breakRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _breakRepository = breakRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        }

    public async Task<BreakResource?> Handle(PutCommand<BreakResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var existingBreak = await _breakRepository.Get(request.Resource.Id);
            if (existingBreak == null)
            {
                throw new KeyNotFoundException($"Break with ID {request.Resource.Id} not found.");
            }

            var updatedBreak = _scheduleMapper.ToBreakEntity(request.Resource);
            updatedBreak.CreateTime = existingBreak.CreateTime;
            updatedBreak.CurrentUserCreated = existingBreak.CurrentUserCreated;
            existingBreak = updatedBreak;
            await _breakRepository.Put(existingBreak);
            await _unitOfWork.CompleteAsync();
            return _scheduleMapper.ToBreakResource(existingBreak);
        }, 
        "updating break", 
        new { BreakId = request.Resource.Id });
    }
}
