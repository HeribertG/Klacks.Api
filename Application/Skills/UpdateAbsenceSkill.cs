// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Updates an existing absence type. Only fields supplied as parameters are changed.
/// nameDe / nameEn / abbreviationDe / abbreviationEn merge into the MultiLanguage value.
/// </summary>
/// <param name="absenceId">Required. UUID of the absence type to update.</param>
/// <param name="nameDe">Optional.</param>
/// <param name="nameEn">Optional.</param>
/// <param name="abbreviationDe">Optional.</param>
/// <param name="abbreviationEn">Optional.</param>
/// <param name="color">Optional hex colour.</param>
/// <param name="defaultLength">Optional default duration in days.</param>
/// <param name="withSaturday">Optional.</param>
/// <param name="withSunday">Optional.</param>
/// <param name="withHoliday">Optional.</param>
/// <param name="isUnpaid">Optional.</param>
/// <param name="hideInGantt">Optional.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_absence")]
public class UpdateAbsenceSkill : BaseSkillImplementation
{
    private readonly IAbsenceRepository _absenceRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateAbsenceSkill(IAbsenceRepository absenceRepository, IUnitOfWork unitOfWork)
    {
        _absenceRepository = absenceRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var absenceId = GetRequiredGuid(parameters, "absenceId");

        var absence = await _absenceRepository.Get(absenceId);
        if (absence == null)
        {
            return SkillResult.Error($"Absence type '{absenceId}' not found.");
        }

        var changed = new List<string>();

        var nameDe = GetParameter<string>(parameters, "nameDe");
        if (!string.IsNullOrWhiteSpace(nameDe) && nameDe != absence.Name.De)
        {
            absence.Name.SetValue("de", nameDe);
            changed.Add("nameDe");
        }

        var nameEn = GetParameter<string>(parameters, "nameEn");
        if (!string.IsNullOrWhiteSpace(nameEn) && nameEn != absence.Name.En)
        {
            absence.Name.SetValue("en", nameEn);
            changed.Add("nameEn");
        }

        var abbreviationDe = GetParameter<string>(parameters, "abbreviationDe");
        if (!string.IsNullOrWhiteSpace(abbreviationDe) && abbreviationDe != absence.Abbreviation.De)
        {
            absence.Abbreviation.SetValue("de", abbreviationDe);
            changed.Add("abbreviationDe");
        }

        var abbreviationEn = GetParameter<string>(parameters, "abbreviationEn");
        if (!string.IsNullOrWhiteSpace(abbreviationEn) && abbreviationEn != absence.Abbreviation.En)
        {
            absence.Abbreviation.SetValue("en", abbreviationEn);
            changed.Add("abbreviationEn");
        }

        var color = GetParameter<string>(parameters, "color");
        if (!string.IsNullOrWhiteSpace(color) && color != absence.Color)
        {
            absence.Color = color;
            changed.Add("color");
        }

        var defaultLength = GetParameter<int?>(parameters, "defaultLength");
        if (defaultLength.HasValue && defaultLength.Value != absence.DefaultLength)
        {
            absence.DefaultLength = defaultLength.Value;
            changed.Add("defaultLength");
        }

        ApplyBoolean(parameters, "withSaturday", v => absence.WithSaturday == v, v => absence.WithSaturday = v, changed, "withSaturday");
        ApplyBoolean(parameters, "withSunday", v => absence.WithSunday == v, v => absence.WithSunday = v, changed, "withSunday");
        ApplyBoolean(parameters, "withHoliday", v => absence.WithHoliday == v, v => absence.WithHoliday = v, changed, "withHoliday");
        ApplyBoolean(parameters, "isUnpaid", v => absence.IsUnpaid == v, v => absence.IsUnpaid = v, changed, "isUnpaid");
        ApplyBoolean(parameters, "hideInGantt", v => absence.HideInGantt == v, v => absence.HideInGantt = v, changed, "hideInGantt");

        if (changed.Count == 0)
        {
            return SkillResult.SuccessResult(
                new { AbsenceId = absenceId, ChangedFields = Array.Empty<string>() },
                "No fields supplied for update — absence type left unchanged.");
        }

        absence.UpdateTime = DateTime.UtcNow;
        absence.CurrentUserUpdated = context.UserName;

        await _absenceRepository.Put(absence);
        await _unitOfWork.CompleteAsync();

        return SkillResult.SuccessResult(
            new
            {
                AbsenceId = absenceId,
                ChangedFields = changed,
                NameDe = absence.Name.De,
                AbbreviationDe = absence.Abbreviation.De
            },
            $"Absence type '{absence.Name.De}' updated ({string.Join(", ", changed)}).");
    }

    private static void ApplyBoolean(
        Dictionary<string, object> parameters,
        string key,
        Func<bool, bool> currentMatches,
        Action<bool> assign,
        List<string> changed,
        string label)
    {
        if (!parameters.ContainsKey(key)) return;
        var value = GetParameter<bool?>(parameters, key);
        if (value == null) return;
        if (currentMatches(value.Value)) return;
        assign(value.Value);
        changed.Add(label);
    }
}
