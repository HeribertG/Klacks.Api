using AutoMapper;
using Klacks.Api.Interfaces;
using Klacks.Api.Queries.Settings.Macros;
using Klacks.Api.Resources.Settings;
using MediatR;

namespace Klacks.Api.Handlers.Settings.Macro
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
