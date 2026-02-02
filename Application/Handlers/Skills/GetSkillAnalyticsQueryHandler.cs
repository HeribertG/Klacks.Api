using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.Skills;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Skills;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Skills;

public class GetSkillAnalyticsQueryHandler : BaseHandler, IRequestHandler<GetSkillAnalyticsQuery, SkillAnalyticsDto>
{
    private readonly ISkillRegistry _skillRegistry;
    private readonly ISkillUsageRepository _usageRepository;
    private readonly SkillMapper _mapper;

    public GetSkillAnalyticsQueryHandler(
        ISkillRegistry skillRegistry,
        ISkillUsageRepository usageRepository,
        SkillMapper mapper,
        ILogger<GetSkillAnalyticsQueryHandler> logger)
        : base(logger)
    {
        _skillRegistry = skillRegistry;
        _usageRepository = usageRepository;
        _mapper = mapper;
    }

    public async Task<SkillAnalyticsDto> Handle(GetSkillAnalyticsQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            _logger.LogInformation("Getting skill analytics for {Days} days", request.Days);

            var skills = _skillRegistry.GetAllSkills();
            var fromDate = DateTime.UtcNow.AddDays(-request.Days);
            var usageRecords = await _usageRepository.GetRecordsAsync(fromDate, cancellationToken);

            var analytics = _mapper.ToAnalyticsDto(skills, usageRecords, request.Days);

            _logger.LogInformation("Returning analytics with {TotalExecutions} total executions",
                analytics.TotalExecutions);

            return analytics;
        }, "getting skill analytics", new { Days = request.Days });
    }
}
