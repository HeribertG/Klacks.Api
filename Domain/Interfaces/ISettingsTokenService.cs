using Klacks.Api.Presentation.DTOs.Filter;

namespace Klacks.Api.Domain.Interfaces;

public interface ISettingsTokenService
{
    /// <summary>
    /// Generates rule tokens by combining states and countries into unified StateCountryToken format
    /// </summary>
    /// <param name="isSelected">Whether tokens should be marked as selected</param>
    /// <returns>Combined list of state and country tokens</returns>
    Task<IEnumerable<StateCountryToken>> GetRuleTokenListAsync(bool isSelected);
}