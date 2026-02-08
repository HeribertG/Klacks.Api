using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.States
{
    public class GetQueryHandler : BaseHandler, IRequestHandler<GetQuery<StateResource>, StateResource>
    {
        private readonly IStateRepository _stateRepository;
        private readonly SettingsMapper _settingsMapper;

        public GetQueryHandler(IStateRepository stateRepository, SettingsMapper settingsMapper, ILogger<GetQueryHandler> logger)
            : base(logger)
        {
            _stateRepository = stateRepository;
            _settingsMapper = settingsMapper;
        }

        public async Task<StateResource> Handle(GetQuery<StateResource> request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(async () =>
            {
                var state = await _stateRepository.Get(request.Id);

                if (state == null)
                {
                    throw new KeyNotFoundException($"State with ID {request.Id} not found");
                }

                return _settingsMapper.ToStateResource(state);
            }, nameof(Handle), new { request.Id });
        }
    }
}
