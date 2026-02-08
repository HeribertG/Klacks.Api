using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.AI;
using Klacks.Api.Domain.Models.AI;
using Klacks.Api.Domain.Models.Skills;
using Klacks.Api.Domain.Services.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

public class AddAiMemorySkill : BaseSkill
{
    private readonly IAiMemoryRepository _aiMemoryRepository;

    public override string Name => "add_ai_memory";

    public override string Description =>
        "Adds a new persistent memory entry for the AI assistant. " +
        "Memories persist across all conversations and help the assistant remember important facts, " +
        "user preferences, and system knowledge.";

    public override SkillCategory Category => SkillCategory.Crud;

    public override IReadOnlyList<string> RequiredPermissions => new[] { "Admin" };

    public override IReadOnlyList<SkillParameter> Parameters => new[]
    {
        new SkillParameter(
            "key",
            "Short identifier or title for the memory entry.",
            SkillParameterType.String,
            Required: true),
        new SkillParameter(
            "content",
            "The actual memory content to remember.",
            SkillParameterType.String,
            Required: true),
        new SkillParameter(
            "category",
            "Category of the memory. Options: user_preference, system_knowledge, learned_fact, workflow, context.",
            SkillParameterType.String,
            Required: false,
            DefaultValue: AiMemoryCategories.LEARNED_FACT),
        new SkillParameter(
            "importance",
            "Importance level from 1 (low) to 10 (high). Higher importance memories are prioritized in the context.",
            SkillParameterType.Integer,
            Required: false,
            DefaultValue: 5)
    };

    public AddAiMemorySkill(IAiMemoryRepository aiMemoryRepository)
    {
        _aiMemoryRepository = aiMemoryRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var key = GetRequiredString(parameters, "key");
        var content = GetRequiredString(parameters, "content");
        var category = GetParameter<string>(parameters, "category", AiMemoryCategories.LEARNED_FACT)!;
        var importance = GetParameter<int?>(parameters, "importance", 5) ?? 5;

        importance = Math.Clamp(importance, 1, 10);

        var memory = new AiMemory
        {
            Id = Guid.NewGuid(),
            Key = key,
            Content = content,
            Category = category,
            Importance = importance,
            Source = "chat"
        };

        await _aiMemoryRepository.AddAsync(memory, cancellationToken);

        return SkillResult.SuccessResult(
            new { MemoryId = memory.Id, Key = key, Category = category, Importance = importance },
            $"Memory added: '{key}' [{category}] (importance: {importance}).");
    }
}
