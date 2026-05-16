// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Creates a new absence type (e.g. Sick Leave, Vacation, Education). This is the
/// stammdaten definition; concrete employee absences are placed via add_break.
/// </summary>
/// <param name="nameDe">Required. German display name.</param>
/// <param name="nameEn">Optional. English display name; falls back to nameDe.</param>
/// <param name="abbreviationDe">Required. Short code (1-6 chars) shown in the schedule grid.</param>
/// <param name="color">Optional. Hex colour like #FF8800; default light grey.</param>
/// <param name="defaultLength">Optional. Default duration in days. Default 1.</param>
/// <param name="withSaturday">Optional. Counts saturdays. Default false.</param>
/// <param name="withSunday">Optional. Counts sundays. Default false.</param>
/// <param name="withHoliday">Optional. Counts holidays. Default false.</param>
/// <param name="isUnpaid">Optional. Unpaid leave. Default false.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("create_absence")]
public class CreateAbsenceSkill : BaseSkillImplementation
{
    private const string DefaultColor = "#CCCCCC";

    private readonly IAbsenceRepository _absenceRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateAbsenceSkill(IAbsenceRepository absenceRepository, IUnitOfWork unitOfWork)
    {
        _absenceRepository = absenceRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var nameDe = GetRequiredString(parameters, "nameDe");
        var abbreviationDe = GetRequiredString(parameters, "abbreviationDe");

        var nameEn = GetParameter<string>(parameters, "nameEn");
        var abbreviationEn = GetParameter<string>(parameters, "abbreviationEn");
        var color = GetParameter<string>(parameters, "color") ?? DefaultColor;
        var defaultLength = GetParameter<int?>(parameters, "defaultLength") ?? 1;
        var defaultValue = GetParameter<decimal?>(parameters, "defaultValue") ?? 0m;
        var withSaturday = GetParameter<bool>(parameters, "withSaturday", false);
        var withSunday = GetParameter<bool>(parameters, "withSunday", false);
        var withHoliday = GetParameter<bool>(parameters, "withHoliday", false);
        var isUnpaid = GetParameter<bool>(parameters, "isUnpaid", false);

        var nameMl = new MultiLanguage();
        nameMl.SetValue("de", nameDe);
        if (!string.IsNullOrWhiteSpace(nameEn))
        {
            nameMl.SetValue("en", nameEn);
        }

        var abbreviationMl = new MultiLanguage();
        abbreviationMl.SetValue("de", abbreviationDe);
        if (!string.IsNullOrWhiteSpace(abbreviationEn))
        {
            abbreviationMl.SetValue("en", abbreviationEn);
        }

        var absence = new Absence
        {
            Id = Guid.NewGuid(),
            Name = nameMl,
            Abbreviation = abbreviationMl,
            Description = MultiLanguage.Empty(),
            Color = color,
            DefaultLength = defaultLength,
            DefaultValue = (double)defaultValue,
            WithSaturday = withSaturday,
            WithSunday = withSunday,
            WithHoliday = withHoliday,
            IsUnpaid = isUnpaid,
            HideInGantt = false,
            Undeletable = false,
            AppliesToContainer = false,
            CreateTime = DateTime.UtcNow,
            CurrentUserCreated = context.UserName
        };

        await _absenceRepository.Add(absence);
        await _unitOfWork.CompleteAsync();

        return SkillResult.SuccessResult(
            new
            {
                AbsenceId = absence.Id,
                NameDe = nameDe,
                AbbreviationDe = abbreviationDe,
                Color = color,
                DefaultLength = defaultLength,
                IsUnpaid = isUnpaid
            },
            $"Absence type '{nameDe}' ({abbreviationDe}) was created.");
    }
}
