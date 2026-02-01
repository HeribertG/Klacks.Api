using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Presentation.DTOs.Staffs;

namespace Klacks.Api.Application.Interfaces;

public interface IClientSearchRepository
{
    Task<Client?> FindByMail(string mail);

    Task<List<Client>> FindList(string? company = null, string? name = null, string? firstname = null);

    Task<string> FindStatePostCode(string zip);

    Task<List<ClientForReplacementResource>> GetClientsForReplacement();
}