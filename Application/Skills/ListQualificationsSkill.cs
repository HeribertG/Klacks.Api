// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists the available qualification master entries (id, name, description) so the agent can pick a
/// qualificationId for set_client_qualification / set_shift_required_qualification. Reads the
/// repository directly (read-only skill, same pattern as list_scenarios).
/// </summary>

using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_qualifications")]
public class ListQualificationsSkill : BaseSkillImplementation
{
    private readonly IQualificationRepository _repository;

    public ListQualificationsSkill(IQualificationRepository repository)
    {
        _repository = repository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var qualifications = await _repository.GetAllAsync(cancellationToken);
        var projected = qualifications
            .Select(q => new { q.Id, q.Name, q.Description })
            .ToList();

        return SkillResult.SuccessResult(
            new { Count = projected.Count, Qualifications = projected },
            $"Found {projected.Count} qualification(s).");
    }
}
