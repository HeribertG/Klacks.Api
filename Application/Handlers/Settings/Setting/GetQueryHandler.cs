using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Settings.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.Setting
{
    public class GetQueryHandler : IRequestHandler<GetQuery, Klacks.Api.Domain.Models.Settings.Settings?>
    {
        private readonly ISettingsRepository repository;

        public GetQueryHandler(ISettingsRepository repository)
        {
            this.repository = repository;
        }

        public async Task<Klacks.Api.Domain.Models.Settings.Settings?> Handle(GetQuery request, CancellationToken cancellationToken)
        {
            return await repository.GetSetting(request.Type);
        }
    }
}
