using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Skills;
using Klacks.Api.Domain.Services.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

public class GetOwnerAddressSkill : BaseSkill
{
    private readonly ISettingsRepository _settingsRepository;

    public override string Name => "get_owner_address";

    public override string Description =>
        "Reads the current owner/company address from the settings. " +
        "Returns all address fields: name, supplement, street, zip, city, state, country, phone, email.";

    public override SkillCategory Category => SkillCategory.Query;

    public override IReadOnlyList<string> RequiredPermissions => new[] { "CanViewSettings" };

    public override IReadOnlyList<SkillParameter> Parameters => Array.Empty<SkillParameter>();

    public GetOwnerAddressSkill(ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var addressName = await GetSettingValue(Constants.Settings.APP_ADDRESS_NAME);
        var supplement = await GetSettingValue(Constants.Settings.APP_ADDRESS_SUPPLEMENT);
        var street = await GetSettingValue(Constants.Settings.APP_ADDRESS_ADDRESS);
        var zip = await GetSettingValue(Constants.Settings.APP_ADDRESS_ZIP);
        var place = await GetSettingValue(Constants.Settings.APP_ADDRESS_PLACE);
        var state = await GetSettingValue(Constants.Settings.APP_ADDRESS_STATE);
        var country = await GetSettingValue(Constants.Settings.APP_ADDRESS_COUNTRY);
        var phone = await GetSettingValue(Constants.Settings.APP_ADDRESS_PHONE);
        var email = await GetSettingValue(Constants.Settings.APP_ADDRESS_MAIL);

        var resultData = new
        {
            AddressName = addressName,
            SupplementAddress = supplement,
            Street = street,
            Zip = zip,
            City = place,
            State = state,
            Country = country,
            Phone = phone,
            Email = email
        };

        return SkillResult.SuccessResult(resultData,
            $"Owner address: {addressName}, {street}, {zip} {place}, State: {state}, Country: {country}");
    }

    private async Task<string> GetSettingValue(string settingType)
    {
        var setting = await _settingsRepository.GetSetting(settingType);
        return setting?.Value ?? "";
    }
}
