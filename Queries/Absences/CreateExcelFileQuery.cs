using Klacks_api.Resources;
using MediatR;

namespace Klacks_api.Queries.Absences
{
  public record CreateExcelFileQuery(string Language) : IRequest<HttpResultResource>;
}
