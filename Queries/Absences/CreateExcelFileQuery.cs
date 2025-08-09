using Klacks.Api.Presentation.Resources;
using MediatR;

namespace Klacks.Api.Queries.Absences
{
    public record CreateExcelFileQuery(string Language) : IRequest<HttpResultResource>;
}
