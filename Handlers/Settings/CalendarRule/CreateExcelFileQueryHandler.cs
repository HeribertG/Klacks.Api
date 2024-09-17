using Klacks_api.Interfaces;
using Klacks_api.Queries.Settings.CalendarRules;
using Klacks_api.Resources;
using MediatR;

namespace Klacks_api.Handlers.Settings.CalendarRule
{
  public class CreateExcelFileQueryHandler : IRequestHandler<CreateExcelFileQuery, HttpResultResource>
  {
    private readonly ISettingsRepository _repository;

    public CreateExcelFileQueryHandler(ISettingsRepository repository)
    {
      _repository = repository;
    }

    public async Task<HttpResultResource> Handle(CreateExcelFileQuery request, CancellationToken cancellationToken)
    {
      return await Task.Factory.StartNew(() => _repository.CreateExcelFile(request.Filter));
    }
  }
}
