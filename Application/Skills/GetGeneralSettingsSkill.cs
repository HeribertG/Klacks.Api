using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Skills;
using Klacks.Api.Domain.Services.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

public class GetGeneralSettingsSkill : BaseSkill
{
    private readonly ISettingsRepository _settingsRepository;

    public override string Name => "get_general_settings";

    public override string Description =>
        "Retrieves the general application settings including the application name. " +
        "Use this to check what the application is called or to verify settings before updating them.";

    public override SkillCategory Category => SkillCategory.Query;

    public override IReadOnlyList<string> RequiredPermissions => new[] { "CanViewSettings" };

    public override IReadOnlyList<SkillParameter> Parameters => Array.Empty<SkillParameter>();

    public GetGeneralSettingsSkill(ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var appNameSetting = await _settingsRepository.GetSetting(Constants.Settings.APP_NAME);

        var resultData = new
        {
            AppName = appNameSetting?.Value ?? "",
            SettingsCard = "General",
            AvailableFields = new[]
            {
                new { Field = "appName", Description = "Application name displayed in the browser title and header", CurrentValue = appNameSetting?.Value ?? "" }
            }
        };

        return SkillResult.SuccessResult(resultData, $"General settings retrieved. App name: '{appNameSetting?.Value ?? "(not set)"}'");
    }
}
