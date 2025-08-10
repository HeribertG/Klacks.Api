using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Settings.CalendarRules;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.CalendarRule
{
    public class ListQueryHandler : IRequestHandler<ListQuery, IEnumerable<Klacks.Api.Domain.Models.Settings.CalendarRule>>
    {
        private readonly IMapper mapper;
        private readonly ISettingsRepository repository;

        public ListQueryHandler(IMapper mapper, ISettingsRepository repository)
        {
            this.mapper = mapper;
            this.repository = repository;
        }

        public async Task<IEnumerable<Klacks.Api.Domain.Models.Settings.CalendarRule>> Handle(ListQuery request, CancellationToken cancellationToken)
        {
            return await repository.GetCalendarRuleList();
        }
    }
}
