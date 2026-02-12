using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.SchedulingRules;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<SchedulingRuleResource>, SchedulingRuleResource?>
{
    private readonly ISchedulingRuleRepository _repository;
    private readonly ScheduleMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCommandHandler(
        ISchedulingRuleRepository repository,
        ScheduleMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<SchedulingRuleResource?> Handle(DeleteCommand<SchedulingRuleResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var existing = await _repository.Get(request.Id);
            if (existing == null)
            {
                throw new KeyNotFoundException($"Scheduling rule with ID {request.Id} not found.");
            }

            var resource = _mapper.ToSchedulingRuleResource(existing);
            await _repository.Delete(request.Id);
            await _unitOfWork.CompleteAsync();

            return resource;
        },
        "deleting scheduling rule",
        new { RuleId = request.Id });
    }
}
