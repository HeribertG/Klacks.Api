using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Settings.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.Setting
{
    public class ListQueryHandler : IRequestHandler<ListQuery, IEnumerable<Klacks.Api.Domain.Models.Settings.Settings>>
    {
        private readonly ISettingsRepository repository;

        public ListQueryHandler(ISettingsRepository repository)
        {
            this.repository = repository;
        }

        public async Task<IEnumerable<Klacks.Api.Domain.Models.Settings.Settings>> Handle(ListQuery request, CancellationToken cancellationToken) => await repository.GetSettingsList();
    }
}
