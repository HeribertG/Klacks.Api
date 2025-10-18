using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Models.Filters;

namespace Klacks.Api.Application.Interfaces;

public interface IClientBreakRepository
{
    Task<(List<Client> Clients, int TotalCount)> BreakList(BreakFilter filter);
}