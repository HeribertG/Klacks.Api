// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that lists all available absence types (e.g. Vacation, Sick Leave) with their configuration.
/// Returns name, abbreviation, color, default duration and visibility settings.
/// </summary>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_absence_types")]
public class ListAbsenceTypesSkill : BaseSkillImplementation
{
    private readonly IAbsenceRepository _absenceRepository;

    public ListAbsenceTypesSkill(IAbsenceRepository absenceRepository)
    {
        _absenceRepository = absenceRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var language = GetParameter<string>(parameters, "language") ?? "de";
        var includeHidden = GetParameter<bool>(parameters, "includeHidden", false);

        var allAbsences = await _absenceRepository.List();

        var absences = allAbsences
            .Where(a => !a.IsDeleted)
            .Where(a => includeHidden || !a.HideInGantt)
            .OrderBy(a => a.Name.GetValue(language) ?? a.Name.De ?? "")
            .Select(a => new
            {
                a.Id,
                Name = a.Name.GetValue(language) ?? a.Name.De,
                Abbreviation = a.Abbreviation.GetValue(language) ?? a.Abbreviation.De,
                a.Color,
                a.DefaultLength,
                a.DefaultValue,
                a.HideInGantt,
                a.WithSaturday,
                a.WithSunday,
                a.WithHoliday
            })
            .ToList();

        var resultData = new
        {
            AbsenceTypes = absences,
            TotalCount = absences.Count
        };

        var message = absences.Count == 0
            ? "No absence types configured."
            : $"Found {absences.Count} absence type(s)" +
              (includeHidden ? " (including hidden)." : ".");

        return SkillResult.SuccessResult(resultData, message);
    }
}
