using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Skills;

public class UpdateGeneralSettingsSkill : BaseSkill
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly IUnitOfWork _unitOfWork;

    public override string Name => "update_general_settings";

    public override string Description =>
        "Updates general application settings. Currently supports changing the application name. " +
        "The application name is displayed in the browser title bar and the application header.";

    public override SkillCategory Category => SkillCategory.Crud;

    public override IReadOnlyList<string> RequiredPermissions => new[] { "CanEditSettings" };

    public override IReadOnlyList<SkillParameter> Parameters => new[]
    {
        new SkillParameter(
            "appName",
            "The new application name to set. This is displayed in the browser title and application header.",
            SkillParameterType.String,
            Required: true)
    };

    public UpdateGeneralSettingsSkill(
        ISettingsRepository settingsRepository,
        IUnitOfWork unitOfWork)
    {
        _settingsRepository = settingsRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var appName = GetRequiredString(parameters, "appName");

        var existingSetting = await _settingsRepository.GetSetting(Constants.Settings.APP_NAME);

        if (existingSetting != null)
        {
            var previousValue = existingSetting.Value;
            existingSetting.Value = appName;
            await _settingsRepository.PutSetting(existingSetting);
            await _unitOfWork.CompleteAsync();

            return SkillResult.SuccessResult(
                new { AppName = appName, PreviousValue = previousValue },
                $"Application name updated from '{previousValue}' to '{appName}'.");
        }

        var newSetting = new Domain.Models.Settings.Settings
        {
            Id = Guid.NewGuid(),
            Type = Constants.Settings.APP_NAME,
            Value = appName
        };

        await _settingsRepository.AddSetting(newSetting);
        await _unitOfWork.CompleteAsync();

        return SkillResult.SuccessResult(
            new { AppName = appName, PreviousValue = "" },
            $"Application name set to '{appName}'.");
    }
}
