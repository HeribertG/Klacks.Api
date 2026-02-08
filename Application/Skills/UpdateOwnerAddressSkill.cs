using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Skills;
using Klacks.Api.Domain.Services.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

public class UpdateOwnerAddressSkill : BaseSkill
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly IUnitOfWork _unitOfWork;

    public override string Name => "update_owner_address";

    public override string Description =>
        "Updates the owner/company address in settings. All fields are optional - only provided fields will be updated. " +
        "IMPORTANT: state and country are REQUIRED and must always be provided. " +
        "Use validate_address first to verify the address and detect the state/canton.";

    public override SkillCategory Category => SkillCategory.Crud;

    public override IReadOnlyList<string> RequiredPermissions => new[] { "CanEditSettings" };

    public override IReadOnlyList<SkillParameter> Parameters => new[]
    {
        new SkillParameter("addressName", "Company or owner name", SkillParameterType.String, Required: false),
        new SkillParameter("supplementAddress", "Additional address line", SkillParameterType.String, Required: false),
        new SkillParameter("street", "Street and house number", SkillParameterType.String, Required: false),
        new SkillParameter("zip", "Postal code / ZIP", SkillParameterType.String, Required: false),
        new SkillParameter("city", "City name", SkillParameterType.String, Required: false),
        new SkillParameter("state", "State or canton abbreviation (e.g. BE, ZH)", SkillParameterType.String, Required: true),
        new SkillParameter("country", "Country abbreviation (e.g. CH, DE, AT)", SkillParameterType.String, Required: true),
        new SkillParameter("phone", "Phone number", SkillParameterType.String, Required: false),
        new SkillParameter("email", "Email address", SkillParameterType.String, Required: false),
    };

    public UpdateOwnerAddressSkill(
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
        var state = GetRequiredString(parameters, "state");
        var country = GetRequiredString(parameters, "country");

        var fieldMap = new Dictionary<string, string>
        {
            { "addressName", Constants.Settings.APP_ADDRESS_NAME },
            { "supplementAddress", Constants.Settings.APP_ADDRESS_SUPPLEMENT },
            { "street", Constants.Settings.APP_ADDRESS_ADDRESS },
            { "zip", Constants.Settings.APP_ADDRESS_ZIP },
            { "city", Constants.Settings.APP_ADDRESS_PLACE },
            { "state", Constants.Settings.APP_ADDRESS_STATE },
            { "country", Constants.Settings.APP_ADDRESS_COUNTRY },
            { "phone", Constants.Settings.APP_ADDRESS_PHONE },
            { "email", Constants.Settings.APP_ADDRESS_MAIL },
        };

        var updatedFields = new List<string>();

        foreach (var (paramName, settingKey) in fieldMap)
        {
            var value = GetParameter<string>(parameters, paramName);
            if (value == null) continue;

            await UpsertSetting(settingKey, value);
            updatedFields.Add(paramName);
        }

        await _unitOfWork.CompleteAsync();

        return SkillResult.SuccessResult(
            new { UpdatedFields = updatedFields, State = state, Country = country },
            $"Owner address updated ({updatedFields.Count} fields). State: {state}, Country: {country}");
    }

    private async Task UpsertSetting(string settingType, string value)
    {
        var existing = await _settingsRepository.GetSetting(settingType);
        if (existing != null)
        {
            existing.Value = value;
            await _settingsRepository.PutSetting(existing);
        }
        else
        {
            var newSetting = new Domain.Models.Settings.Settings
            {
                Id = Guid.NewGuid(),
                Type = settingType,
                Value = value
            };
            await _settingsRepository.AddSetting(newSetting);
        }
    }
}
