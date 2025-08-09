using AutoMapper;
using Klacks.Api.Interfaces;
using Klacks.Api.Application.Queries.Settings.Macros;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.Macro
{
    public class GetQueryHandler : IRequestHandler<GetQuery, MacroResource?>
    {
        private readonly IMapper mapper;
        private readonly ISettingsRepository repository;

        public GetQueryHandler(IMapper mapper, ISettingsRepository repository)
        {
            this.mapper = mapper;
            this.repository = repository;
        }

        public async Task<MacroResource?> Handle(GetQuery request, CancellationToken cancellationToken)
        {
            var macro = await repository.GetMacro(request.Id);

            return mapper.Map<Models.Settings.Macro, MacroResource>(macro);
        }
    }
}
