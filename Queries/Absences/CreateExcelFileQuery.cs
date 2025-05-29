using Klacks.Api.Resources;
using MediatR;

namespace Klacks.Api.Queries.Absences
{
    public record CreateExcelFileQuery(string Language) : IRequest<HttpResultResource>;
}
