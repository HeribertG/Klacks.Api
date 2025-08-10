using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Settings.CalendarRules;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.CalendarRule
{
    public class GetQueryHandler : IRequestHandler<GetQuery, Klacks.Api.Domain.Models.Settings.CalendarRule?>
    {
        private readonly IMapper mapper;
        private readonly ISettingsRepository repository;

        public GetQueryHandler(IMapper mapper, ISettingsRepository repository)
        {
            this.mapper = mapper;
            this.repository = repository;
        }

        public async Task<Klacks.Api.Domain.Models.Settings.CalendarRule?> Handle(GetQuery request, CancellationToken cancellationToken)
        {
            return await repository.GetCalendarRule(request.Id);
        }
    }
}
