using Klacks_api.Interfaces;
using Klacks_api.Queries.Absences;
using Klacks_api.Resources;
using MediatR;

namespace Klacks_api.Handlers.Absences;

public class CreateExcelFileQueryHandler : IRequestHandler<CreateExcelFileQuery, HttpResultResource>
{
  private readonly IAbsenceRepository repository;

  public CreateExcelFileQueryHandler(IAbsenceRepository repository)
  {
    this.repository = repository;
  }

  public async Task<HttpResultResource> Handle(CreateExcelFileQuery request, CancellationToken cancellationToken)
  {
    return await Task.Factory.StartNew(() => repository.CreateExcelFile(request.Language));
  }
}
