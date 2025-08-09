using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Settings.MacrosTypes;
using MediatR;

namespace Klacks.Api.Application.Handlers.Settings.MacrosTypes;

public class GetQueryHandler : IRequestHandler<GetQuery, Models.Settings.MacroType?>
{
    private readonly IMapper mapper;
    private readonly ISettingsRepository repository;

    public GetQueryHandler(IMapper mapper, ISettingsRepository repository)
    {
        this.mapper = mapper;
        this.repository = repository;
    }

    public async Task<Models.Settings.MacroType?> Handle(GetQuery request, CancellationToken cancellationToken)
    {
        return await repository.GetMacroType(request.Id);
    }
}
