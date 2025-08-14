using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Settings.Macros;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.Macro
{
    public class GetQueryHandler : IRequestHandler<GetQuery, MacroResource?>
    {
        private readonly ISettingsRepository _settingsRepository;
        private readonly IMapper _mapper;

        public GetQueryHandler(ISettingsRepository settingsRepository, IMapper mapper)
        {
            _settingsRepository = settingsRepository;
            _mapper = mapper;
        }

        public async Task<MacroResource?> Handle(GetQuery request, CancellationToken cancellationToken)
        {
            var macro = await _settingsRepository.GetMacro(request.Id);
            return _mapper.Map<MacroResource>(macro);
        }
    }
}
