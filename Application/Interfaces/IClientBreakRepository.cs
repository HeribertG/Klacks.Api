using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Models.Filters;

namespace Klacks.Api.Application.Interfaces;

public interface IClientBreakRepository
{
    Task<List<Client>> BreakList(BreakFilter filter);
}