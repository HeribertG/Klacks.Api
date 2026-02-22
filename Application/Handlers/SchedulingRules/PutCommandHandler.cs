using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.SchedulingRules;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<SchedulingRuleResource>, SchedulingRuleResource?>
{
    private readonly ISchedulingRuleRepository _repository;
    private readonly ScheduleMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public PutCommandHandler(
        ISchedulingRuleRepository repository,
        ScheduleMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<SchedulingRuleResource?> Handle(PutCommand<SchedulingRuleResource> request, CancellationToken cancellationToken)
    {
        Validate(request.Resource);

        return await ExecuteAsync(async () =>
        {
            var existing = await _repository.Get(request.Resource.Id);
            if (existing == null)
            {
                throw new KeyNotFoundException($"Scheduling rule with ID {request.Resource.Id} not found.");
            }

            _mapper.UpdateSchedulingRuleEntity(existing, request.Resource);
            await _unitOfWork.CompleteAsync();

            return _mapper.ToSchedulingRuleResource(existing);
        },
        "updating scheduling rule",
        new { RuleId = request.Resource.Id, request.Resource.Name });
    }

    private static void Validate(SchedulingRuleResource? resource)
    {
        if (resource == null)
        {
            throw new InvalidRequestException("Scheduling rule data is required.");
        }

        if (string.IsNullOrWhiteSpace(resource.Name))
        {
            throw new InvalidRequestException("Name is required.");
        }
    }
}
