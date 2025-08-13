using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Presentation.DTOs.Settings;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services;

public class SettingsApplicationService
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly IStateRepository _stateRepository;
    private readonly ICountryRepository _countryRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<SettingsApplicationService> _logger;

    public SettingsApplicationService(
        ISettingsRepository settingsRepository,
        IStateRepository stateRepository,
        ICountryRepository countryRepository,
        IMapper mapper,
        ILogger<SettingsApplicationService> logger)
    {
        _settingsRepository = settingsRepository;
        _stateRepository = stateRepository;
        _countryRepository = countryRepository;
        _mapper = mapper;
        _logger = logger;
    }

    #region Settings Operations

    public async Task<IEnumerable<Klacks.Api.Domain.Models.Settings.Settings>> GetAllSettingsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting all settings");
        var settings = await _settingsRepository.GetSettingsList();
        _logger.LogInformation("Retrieved {Count} settings", settings.Count());
        return settings;
    }

    public async Task<Klacks.Api.Domain.Models.Settings.Settings?> GetSettingByTypeAsync(string type, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting setting by type: {Type}", type);
        var setting = await _settingsRepository.GetSetting(type);
        
        if (setting == null)
        {
            _logger.LogWarning("Setting of type {Type} not found", type);
        }
        
        return setting;
    }

    public async Task<Klacks.Api.Domain.Models.Settings.Settings> CreateSettingAsync(Klacks.Api.Domain.Models.Settings.Settings setting, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating new setting");
        var createdSetting = await _settingsRepository.AddSetting(setting);
        _logger.LogInformation("Setting created successfully");
        return createdSetting;
    }

    public async Task<Klacks.Api.Domain.Models.Settings.Settings> UpdateSettingAsync(Klacks.Api.Domain.Models.Settings.Settings setting, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating setting");
        var updatedSetting = await _settingsRepository.PutSetting(setting);
        _logger.LogInformation("Setting updated successfully");
        return updatedSetting;
    }

    #endregion Settings Operations

    #region MacroType Operations

    public async Task<IEnumerable<MacroType>> GetAllMacroTypesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting all macro types");
        var macroTypes = await _settingsRepository.GetOriginalMacroTypeList();
        _logger.LogInformation("Retrieved {Count} macro types", macroTypes.Count());
        return macroTypes;
    }

    public async Task<MacroType?> GetMacroTypeByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting macro type by ID: {MacroTypeId}", id);
        var macroType = await _settingsRepository.GetMacroType(id);
        
        if (macroType == null)
        {
            _logger.LogWarning("Macro type with ID {MacroTypeId} not found", id);
        }
        
        return macroType;
    }

    public async Task<MacroType> CreateMacroTypeAsync(MacroType macroType, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating new macro type");
        var createdMacroType = _settingsRepository.AddMacroType(macroType);
        _logger.LogInformation("Macro type created successfully");
        return createdMacroType;
    }

    public async Task<MacroType> UpdateMacroTypeAsync(MacroType macroType, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating macro type: {MacroTypeId}", macroType.Id);
        var updatedMacroType = _settingsRepository.PutMacroType(macroType);
        _logger.LogInformation("Macro type updated successfully");
        return updatedMacroType;
    }

    public async Task<MacroType?> DeleteMacroTypeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting macro type: {MacroTypeId}", id);
        var deletedMacroType = await _settingsRepository.DeleteMacroType(id);
        
        if (deletedMacroType != null)
        {
            _logger.LogInformation("Macro type deleted successfully");
        }
        else
        {
            _logger.LogWarning("Macro type with ID {MacroTypeId} not found for deletion", id);
        }
        
        return deletedMacroType;
    }

    #endregion MacroType Operations

    #region Macro Operations

    public async Task<IEnumerable<MacroResource>> GetAllMacrosAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting all macros");
        var macros = await _settingsRepository.GetMacroList();
        _logger.LogInformation("Retrieved {Count} macros", macros.Count());
        return _mapper.Map<IEnumerable<MacroResource>>(macros);
    }

    public async Task<MacroResource?> GetMacroByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting macro by ID: {MacroId}", id);
        var macro = await _settingsRepository.GetMacro(id);
        
        if (macro == null)
        {
            _logger.LogWarning("Macro with ID {MacroId} not found", id);
            return null;
        }
        
        return _mapper.Map<MacroResource>(macro);
    }

    public async Task<MacroResource> CreateMacroAsync(MacroResource macroResource, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating new macro");
        var macro = _mapper.Map<Macro>(macroResource);
        var createdMacro = _settingsRepository.AddMacro(macro);
        _logger.LogInformation("Macro created successfully");
        return _mapper.Map<MacroResource>(createdMacro);
    }

    public async Task<MacroResource> UpdateMacroAsync(MacroResource macroResource, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating macro: {MacroId}", macroResource.Id);
        var macro = _mapper.Map<Macro>(macroResource);
        var updatedMacro = _settingsRepository.PutMacro(macro);
        _logger.LogInformation("Macro updated successfully");
        return _mapper.Map<MacroResource>(updatedMacro);
    }

    public async Task<MacroResource> DeleteMacroAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting macro: {MacroId}", id);
        var deletedMacro = await _settingsRepository.DeleteMacro(id);
        _logger.LogInformation("Macro deleted successfully");
        return _mapper.Map<MacroResource>(deletedMacro);
    }

    #endregion Macro Operations

    #region Vat Operations

    public async Task<IEnumerable<Vat>> GetAllVatsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting all VATs");
        var vats = await _settingsRepository.GetVATList();
        _logger.LogInformation("Retrieved {Count} VATs", vats.Count());
        return vats;
    }

    public async Task<Vat?> GetVatByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting VAT by ID: {VatId}", id);
        var vat = await _settingsRepository.GetVAT(id);
        
        if (vat == null)
        {
            _logger.LogWarning("VAT with ID {VatId} not found", id);
        }
        
        return vat;
    }

    public async Task<Vat> CreateVatAsync(Vat vat, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating new VAT");
        var createdVat = _settingsRepository.AddVAT(vat);
        _logger.LogInformation("VAT created successfully");
        return createdVat;
    }

    public async Task<Vat> UpdateVatAsync(Vat vat, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating VAT: {VatId}", vat.Id);
        var updatedVat = _settingsRepository.PutVAT(vat);
        _logger.LogInformation("VAT updated successfully");
        return updatedVat;
    }

    public async Task<Vat?> DeleteVatAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting VAT: {VatId}", id);
        var deletedVat = await _settingsRepository.DeleteVAT(id);
        
        if (deletedVat != null)
        {
            _logger.LogInformation("VAT deleted successfully");
        }
        else
        {
            _logger.LogWarning("VAT with ID {VatId} not found for deletion", id);
        }
        
        return deletedVat;
    }

    #endregion Vat Operations

    #region CalendarRule Operations

    public async Task<IEnumerable<CalendarRule>> GetAllCalendarRulesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting all calendar rules");
        var calendarRules = await _settingsRepository.GetCalendarRuleList();
        _logger.LogInformation("Retrieved {Count} calendar rules", calendarRules.Count());
        return calendarRules;
    }

    public async Task<CalendarRule?> GetCalendarRuleByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting calendar rule by ID: {CalendarRuleId}", id);
        var calendarRule = await _settingsRepository.GetCalendarRule(id);
        
        if (calendarRule == null)
        {
            _logger.LogWarning("Calendar rule with ID {CalendarRuleId} not found", id);
        }
        
        return calendarRule;
    }

    public async Task<TruncatedCalendarRule> GetTruncatedCalendarRulesAsync(CalendarRulesFilter filter, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting truncated calendar rules with filter");
        var result = await _settingsRepository.GetTruncatedCalendarRuleList(filter);
        _logger.LogInformation("Truncated calendar rules retrieved successfully");
        return result;
    }

    public async Task<IEnumerable<StateCountryToken>> GetRuleTokenListAsync(bool isSelected, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting rule token list with isSelected: {IsSelected}", isSelected);
        var states = await _stateRepository.List();
        var countries = await _countryRepository.List();
        var tokens = new List<StateCountryToken>();
        
        foreach (var state in states)
        {
            tokens.Add(new StateCountryToken
            {
                Id = state.Id,
                State = state.Abbreviation,
                StateName = state.Name,
                Country = string.Empty,
                CountryName = new MultiLanguage(),
                Select = isSelected
            });
        }
        
        foreach (var country in countries)
        {
            tokens.Add(new StateCountryToken
            {
                Id = country.Id,
                Country = country.Abbreviation,
                CountryName = country.Name,
                State = string.Empty,
                StateName = new MultiLanguage(),
                Select = isSelected
            });
        }
        
        _logger.LogInformation("Retrieved {Count} rule tokens", tokens.Count);
        return tokens;
    }

    public async Task<CalendarRule> CreateCalendarRuleAsync(CalendarRuleResource calendarRuleResource, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating new calendar rule");
        var calendarRule = _mapper.Map<CalendarRule>(calendarRuleResource);
        var createdCalendarRule = _settingsRepository.AddCalendarRule(calendarRule);
        _logger.LogInformation("Calendar rule created successfully");
        return createdCalendarRule;
    }

    public async Task UpdateCalendarRuleAsync(CalendarRule calendarRule, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating calendar rule: {CalendarRuleId}", calendarRule.Id);
        _settingsRepository.PutCalendarRule(calendarRule);
        _logger.LogInformation("Calendar rule updated successfully");
    }

    public async Task<CalendarRule?> DeleteCalendarRuleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting calendar rule: {CalendarRuleId}", id);
        var deletedCalendarRule = await _settingsRepository.DeleteCalendarRule(id);
        
        if (deletedCalendarRule != null)
        {
            _logger.LogInformation("Calendar rule deleted successfully");
        }
        else
        {
            _logger.LogWarning("Calendar rule with ID {CalendarRuleId} not found for deletion", id);
        }
        
        return deletedCalendarRule;
    }

    #endregion CalendarRule Operations
}