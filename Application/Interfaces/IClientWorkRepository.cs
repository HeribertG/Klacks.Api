using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Models.Filters;

namespace Klacks.Api.Application.Interfaces;

public interface IClientWorkRepository
{
    Task<List<Client>> WorkList(WorkFilter filter);
}