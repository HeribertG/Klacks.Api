using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.BreakPlaceholders
{
    public class GetQueryHandler : BaseHandler, IRequestHandler<GetQuery<BreakPlaceholderResource>, BreakPlaceholderResource>
    {
        private readonly IBreakPlaceholderRepository _breakPlaceholderRepository;
        private readonly ScheduleMapper _scheduleMapper;

        public GetQueryHandler(IBreakPlaceholderRepository breakPlaceholderRepository, ScheduleMapper scheduleMapper, ILogger<GetQueryHandler> logger)
            : base(logger)
        {
            _breakPlaceholderRepository = breakPlaceholderRepository;
            _scheduleMapper = scheduleMapper;
        }

        public async Task<BreakPlaceholderResource> Handle(GetQuery<BreakPlaceholderResource> request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(async () =>
            {
                var breakEntity = await _breakPlaceholderRepository.Get(request.Id);

                if (breakEntity == null)
                {
                    throw new KeyNotFoundException($"Break with ID {request.Id} not found");
                }

                return _scheduleMapper.ToBreakPlaceholderResource(breakEntity);
            }, nameof(Handle), new { request.Id });
        }
    }
}
