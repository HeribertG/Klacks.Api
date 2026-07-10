// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Creates a new absence type (master data of the settings page, e.g. Vacation, Sick Leave)
/// with multilingual name/abbreviation, color and day-counting flags. The write runs in a
/// transaction and is verified by re-reading the type; a mismatch rolls it back. This
/// creates the TYPE — booking an absence for an employee goes through create_absence.
/// </summary>
/// <param name="name">Required. German name of the absence type (used for all core languages unless overridden).</param>
/// <param name="abbreviation">Required. Short code shown in the schedule (e.g. FE, KR).</param>
/// <param name="color">Optional. Hex color like #FFAA00 (default #CCCCCC).</param>
/// <param name="defaultLength">Optional. Default duration in days (default 1).</param>
/// <param name="defaultValue">Optional. Default day value 0..1 (default 1 = full day).</param>
/// <param name="withSaturday">Optional. Saturdays count as absence days (default false).</param>
/// <param name="withSunday">Optional. Sundays count as absence days (default false).</param>
/// <param name="withHoliday">Optional. Holidays count as absence days (default false).</param>
/// <param name="isUnpaid">Optional. Absence is unpaid (default false).</param>
/// <param name="hideInGantt">Optional. Hide the type in the absence Gantt (default false).</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class CreateAbsenceTypeSkill : BaseSkillImplementation
{
    private const string SkillName = "create_absence_type";
    private const string DefaultColor = "#CCCCCC";

    private readonly IAbsenceRepository _absenceRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateAbsenceTypeSkill(IAbsenceRepository absenceRepository, IUnitOfWork unitOfWork)
    {
        _absenceRepository = absenceRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var name = GetRequiredString(parameters, "name").Trim();
        var abbreviation = GetRequiredString(parameters, "abbreviation").Trim();
        var color = GetParameter<string>(parameters, "color") ?? DefaultColor;
        var defaultLength = GetParameter<int?>(parameters, "defaultLength") ?? 1;
        var defaultValue = (double)(GetParameter<decimal?>(parameters, "defaultValue") ?? 1m);
        if (defaultLength < 0 || defaultValue < 0 || defaultValue > 1)
        {
            return SkillResult.Error("defaultLength must be >= 0 and defaultValue between 0 and 1.");
        }

        var existing = await _absenceRepository.List();
        var duplicate = existing.FirstOrDefault(a => !a.IsDeleted
            && MultiLanguage.CoreLanguages.Any(l =>
                string.Equals(a.Name.GetValue(l), name, StringComparison.OrdinalIgnoreCase)));
        if (duplicate != null)
        {
            return SkillResult.Error(
                $"An absence type named '{name}' already exists (id {duplicate.Id}) — use update_absence_type.");
        }

        var absence = new Absence
        {
            Id = Guid.NewGuid(),
            Name = Multilingual(name),
            Abbreviation = Multilingual(abbreviation),
            Description = Multilingual(string.Empty),
            Color = color,
            DefaultLength = defaultLength,
            DefaultValue = defaultValue,
            WithSaturday = GetParameter<bool>(parameters, "withSaturday", false),
            WithSunday = GetParameter<bool>(parameters, "withSunday", false),
            WithHoliday = GetParameter<bool>(parameters, "withHoliday", false),
            IsUnpaid = GetParameter<bool>(parameters, "isUnpaid", false),
            HideInGantt = GetParameter<bool>(parameters, "hideInGantt", false),
            CreateTime = DateTime.UtcNow,
            CurrentUserCreated = context.UserName
        };

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await _absenceRepository.Add(absence);
                await _unitOfWork.CompleteAsync();
                await ConfirmPersistedAsync(
                    SkillName,
                    () => _absenceRepository.GetNoTracking(absence.Id),
                    persisted => !persisted.IsDeleted
                        && string.Equals(persisted.Name.De, name, StringComparison.Ordinal),
                    $"the new absence type '{name}'");
                return absence.Id;
            });
        }
        catch (SkillVerificationException ex)
        {
            return SkillResult.Error(ex.Message);
        }

        return SkillResult.SuccessResult(
            new
            {
                AbsenceTypeId = absence.Id,
                Name = name,
                Abbreviation = abbreviation,
                absence.Color,
                absence.DefaultLength,
                absence.DefaultValue,
                absence.WithSaturday,
                absence.WithSunday,
                absence.WithHoliday,
                absence.IsUnpaid
            },
            $"Absence type '{name}' ({abbreviation}) was created and confirmed in the database (verified). " +
            "Employees can now book it; the seeded language packs do not translate custom types automatically.");
    }

    private static MultiLanguage Multilingual(string value)
    {
        var ml = new MultiLanguage();
        foreach (var language in MultiLanguage.CoreLanguages)
        {
            ml.SetValue(language, value);
        }

        return ml;
    }
}
