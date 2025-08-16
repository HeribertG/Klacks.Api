using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Presentation.DTOs.Filter;

namespace Klacks.Api.Domain.Interfaces;

public interface IClientWorkFilterService
{
    IQueryable<Client> FilterByMembershipYearMonth(IQueryable<Client> query, int year, int month);
    IQueryable<Client> FilterByWorkSchedule(IQueryable<Client> query, WorkFilter filter, DataBaseContext context);
    IQueryable<Client> ApplyBreakYearFilter(IQueryable<Client> query, BreakFilter filter);
}