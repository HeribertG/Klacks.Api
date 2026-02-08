using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.AI;
using Klacks.Api.Domain.Models.Skills;
using Klacks.Api.Domain.Services.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

public class GetAiMemoriesSkill : BaseSkill
{
    private readonly IAiMemoryRepository _aiMemoryRepository;

    public override string Name => "get_ai_memories";

    public override string Description =>
        "Retrieves the AI assistant's persistent memory entries. " +
        "Can filter by category or search by keyword. " +
        "Returns all memories sorted by importance.";

    public override SkillCategory Category => SkillCategory.Query;

    public override IReadOnlyList<string> RequiredPermissions => new[] { "Admin" };

    public override IReadOnlyList<SkillParameter> Parameters => new[]
    {
        new SkillParameter(
            "category",
            "Filter by category. Options: user_preference, system_knowledge, learned_fact, workflow, context.",
            SkillParameterType.String,
            Required: false),
        new SkillParameter(
            "searchTerm",
            "Search term to filter memories by key or content.",
            SkillParameterType.String,
            Required: false)
    };

    public GetAiMemoriesSkill(IAiMemoryRepository aiMemoryRepository)
    {
        _aiMemoryRepository = aiMemoryRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var category = GetParameter<string>(parameters, "category");
        var searchTerm = GetParameter<string>(parameters, "searchTerm");

        var memories = !string.IsNullOrWhiteSpace(searchTerm)
            ? await _aiMemoryRepository.SearchAsync(searchTerm, cancellationToken)
            : !string.IsNullOrWhiteSpace(category)
                ? await _aiMemoryRepository.GetByCategoryAsync(category, cancellationToken)
                : await _aiMemoryRepository.GetAllAsync(cancellationToken);

        var result = memories.Select(m => new
        {
            m.Id,
            m.Key,
            m.Content,
            m.Category,
            m.Importance,
            m.Source,
            CreatedAt = m.CreateTime
        }).ToList();

        return SkillResult.SuccessResult(
            new { Memories = result, Count = result.Count },
            $"Found {result.Count} memory entries.");
    }
}
