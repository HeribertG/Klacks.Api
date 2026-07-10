// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Updates an existing absence type (master data): name, abbreviation, color, default
/// duration/value and the day-counting flags. Only supplied fields change. The type is
/// addressed by UUID or by name (lone match across the core languages; ambiguous names
/// return the candidates). The write is verified by re-reading the type.
/// </summary>
/// <param name="absenceTypeId">Optional. UUID of the absence type; takes precedence over name lookup.</param>
/// <param name="typeName">Optional. Current name of the type in any core language.</param>
/// <param name="name">Optional. New name (set for all core languages).</param>
/// <param name="abbreviation">Optional. New short code.</param>
/// <param name="color">Optional. New hex color.</param>
/// <param name="defaultLength">Optional. New default duration in days.</param>
/// <param name="defaultValue">Optional. New default day value 0..1.</param>
/// <param name="withSaturday">Optional. Saturdays count as absence days.</param>
/// <param name="withSunday">Optional. Sundays count as absence days.</param>
/// <param name="withHoliday">Optional. Holidays count as absence days.</param>
/// <param name="isUnpaid">Optional. Absence is unpaid.</param>
/// <param name="hideInGantt">Optional. Hide the type in the absence Gantt.</param>

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
public class UpdateAbsenceTypeSkill : BaseSkillImplementation
{
    private const string SkillName = "update_absence_type";

    private readonly IAbsenceRepository _absenceRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateAbsenceTypeSkill(IAbsenceRepository absenceRepository, IUnitOfWork unitOfWork)
    {
        _absenceRepository = absenceRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var (absence, resolveError) = await AbsenceTypeResolver.ResolveAsync(
            GetParameter<string>(parameters, "absenceTypeId"),
            GetParameter<string>(parameters, "typeName"),
            _absenceRepository);
        if (resolveError != null)
        {
            return SkillResult.Error(resolveError);
        }

        var changed = new List<string>();
        var verifications = new List<Func<Absence, bool>>();

        var name = GetParameter<string>(parameters, "name");
        if (!string.IsNullOrWhiteSpace(name) && name != absence!.Name.De)
        {
            SetAllLanguages(absence.Name, name);
            changed.Add("name");
            verifications.Add(p => string.Equals(p.Name.De, name, StringComparison.Ordinal));
        }

        var abbreviation = GetParameter<string>(parameters, "abbreviation");
        if (!string.IsNullOrWhiteSpace(abbreviation) && abbreviation != absence!.Abbreviation.De)
        {
            SetAllLanguages(absence.Abbreviation, abbreviation);
            changed.Add("abbreviation");
            verifications.Add(p => string.Equals(p.Abbreviation.De, abbreviation, StringComparison.Ordinal));
        }

        var color = GetParameter<string>(parameters, "color");
        if (!string.IsNullOrWhiteSpace(color) && color != absence!.Color)
        {
            absence.Color = color;
            changed.Add("color");
            verifications.Add(p => p.Color == color);
        }

        var defaultLength = GetParameter<int?>(parameters, "defaultLength");
        if (defaultLength.HasValue && defaultLength.Value != absence!.DefaultLength)
        {
            if (defaultLength.Value < 0)
            {
                return SkillResult.Error("defaultLength must be >= 0.");
            }

            absence.DefaultLength = defaultLength.Value;
            changed.Add("defaultLength");
            verifications.Add(p => p.DefaultLength == defaultLength.Value);
        }

        var defaultValueRaw = GetParameter<decimal?>(parameters, "defaultValue");
        if (defaultValueRaw.HasValue)
        {
            var defaultValue = (double)defaultValueRaw.Value;
            if (defaultValue < 0 || defaultValue > 1)
            {
                return SkillResult.Error("defaultValue must be between 0 and 1.");
            }

            if (Math.Abs(absence!.DefaultValue - defaultValue) > double.Epsilon)
            {
                absence.DefaultValue = defaultValue;
                changed.Add("defaultValue");
                verifications.Add(p => Math.Abs(p.DefaultValue - defaultValue) < 0.0001);
            }
        }

        ApplyFlag(parameters, "withSaturday", absence!, changed, verifications,
            a => a.WithSaturday, (a, v) => a.WithSaturday = v, p => p.WithSaturday);
        ApplyFlag(parameters, "withSunday", absence, changed, verifications,
            a => a.WithSunday, (a, v) => a.WithSunday = v, p => p.WithSunday);
        ApplyFlag(parameters, "withHoliday", absence, changed, verifications,
            a => a.WithHoliday, (a, v) => a.WithHoliday = v, p => p.WithHoliday);
        ApplyFlag(parameters, "isUnpaid", absence, changed, verifications,
            a => a.IsUnpaid, (a, v) => a.IsUnpaid = v, p => p.IsUnpaid);
        ApplyFlag(parameters, "hideInGantt", absence, changed, verifications,
            a => a.HideInGantt, (a, v) => a.HideInGantt = v, p => p.HideInGantt);

        if (changed.Count == 0)
        {
            return SkillResult.SuccessResult(
                new { AbsenceTypeId = absence.Id, ChangedFields = Array.Empty<string>() },
                "No fields supplied for update — absence type left unchanged.");
        }

        absence.UpdateTime = DateTime.UtcNow;
        absence.CurrentUserUpdated = context.UserName;

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await _absenceRepository.Put(absence);
                await _unitOfWork.CompleteAsync();
                await ConfirmPersistedAsync(
                    SkillName,
                    () => _absenceRepository.GetNoTracking(absence.Id),
                    persisted => verifications.All(check => check(persisted)),
                    $"the update ({string.Join(", ", changed)}) of absence type '{absence.Name.De}'");
                return true;
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
                ChangedFields = changed,
                Name = absence.Name.De,
                Abbreviation = absence.Abbreviation.De,
                absence.Color
            },
            $"Absence type '{absence.Name.De}' updated ({string.Join(", ", changed)}) and confirmed in the " +
            "database (verified). The change affects future bookings; existing bookings keep their values.");
    }

    private void ApplyFlag(
        Dictionary<string, object> parameters,
        string parameterName,
        Absence absence,
        List<string> changed,
        List<Func<Absence, bool>> verifications,
        Func<Absence, bool> get,
        Action<Absence, bool> set,
        Func<Absence, bool> persistedGet)
    {
        var value = GetParameter<bool?>(parameters, parameterName);
        if (!value.HasValue || get(absence) == value.Value)
        {
            return;
        }

        set(absence, value.Value);
        changed.Add(parameterName);
        verifications.Add(p => persistedGet(p) == value.Value);
    }

    private static void SetAllLanguages(MultiLanguage target, string value)
    {
        foreach (var language in MultiLanguage.CoreLanguages)
        {
            target.SetValue(language, value);
        }
    }
}
