using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Application.DTOs.Assistant;
using Riok.Mapperly.Abstractions;

namespace Klacks.Api.Application.Mappers;

[Mapper]
public partial class SkillMapper
{
    public SkillDto ToDto(ISkill skill)
    {
        return new SkillDto
        {
            Name = skill.Name,
            Description = skill.Description,
            Category = skill.Category,
            Parameters = skill.Parameters.Select(ToParameterDto).ToList(),
            RequiredPermissions = skill.RequiredPermissions.ToList()
        };
    }

    public List<SkillDto> ToDtos(IEnumerable<ISkill> skills)
    {
        return skills.Select(ToDto).ToList();
    }

    public SkillParameterDto ToParameterDto(SkillParameter parameter)
    {
        return new SkillParameterDto
        {
            Name = parameter.Name,
            Description = parameter.Description,
            Type = parameter.Type,
            Required = parameter.Required,
            DefaultValue = parameter.DefaultValue,
            EnumValues = parameter.EnumValues?.ToList()
        };
    }

    public SkillExecuteResponse ToResponse(SkillResult result)
    {
        return new SkillExecuteResponse
        {
            Success = result.Success,
            Data = result.Data,
            Message = result.Message,
            ResultType = result.Type,
            Metadata = result.Metadata
        };
    }

    public List<SkillExecuteResponse> ToResponses(IEnumerable<SkillResult> results)
    {
        return results.Select(ToResponse).ToList();
    }

    public SkillInvocation ToInvocation(SkillExecuteRequest request)
    {
        return new SkillInvocation
        {
            SkillName = request.SkillName,
            Parameters = request.Parameters
        };
    }

    public SkillInvocation ToInvocation(SkillInvocationDto request)
    {
        return new SkillInvocation
        {
            SkillName = request.SkillName,
            Parameters = request.Parameters,
            StopOnError = request.StopOnError
        };
    }

    public List<SkillInvocation> ToInvocations(IEnumerable<SkillInvocationDto> requests)
    {
        return requests.Select(ToInvocation).ToList();
    }

    public SkillExecutionContext ToContext(
        Guid userId,
        Guid tenantId,
        string userName,
        List<string> userPermissions)
    {
        return new SkillExecutionContext
        {
            UserId = userId,
            TenantId = tenantId,
            UserName = userName,
            UserPermissions = userPermissions,
            UserTimezone = "Europe/Zurich"
        };
    }

    public SkillAnalyticsDto ToAnalyticsDto(
        IReadOnlyList<ISkill> skills,
        IReadOnlyList<SkillUsageRecord> usageRecords,
        int days)
    {
        var totalExecutions = usageRecords.Count;
        var successfulExecutions = usageRecords.Count(r => r.Success);

        return new SkillAnalyticsDto
        {
            TotalExecutions = totalExecutions,
            SuccessRate = totalExecutions > 0
                ? (decimal)successfulExecutions / totalExecutions * 100
                : 100m,
            MostUsedSkills = usageRecords
                .GroupBy(r => r.SkillName)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => new SkillUsageSummaryDto
                {
                    SkillName = g.Key,
                    ExecutionCount = g.Count(),
                    SuccessRate = g.Count() > 0
                        ? (decimal)g.Count(r => r.Success) / g.Count() * 100
                        : 100m,
                    AvgDurationMs = g.Average(r => r.DurationMs)
                })
                .ToList(),
            UsageByCategory = skills
                .GroupBy(s => s.Category.ToString())
                .ToDictionary(g => g.Key, g => g.Count()),
            UsageOverTime = Enumerable.Range(0, Math.Min(days, 30))
                .Select(d =>
                {
                    var date = DateOnly.FromDateTime(DateTime.Today.AddDays(-d));
                    var dayRecords = usageRecords.Where(r =>
                        DateOnly.FromDateTime(r.Timestamp) == date).ToList();
                    return new DailyUsageDto
                    {
                        Date = date,
                        Executions = dayRecords.Count,
                        Successes = dayRecords.Count(r => r.Success)
                    };
                })
                .ToList()
        };
    }
}
