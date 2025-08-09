using Klacks.Api.Presentation.DTOs;
using MediatR;

namespace Klacks.Api.Queries.Absences
{
    public record CreateExcelFileQuery(string Language) : IRequest<HttpResultResource>;
}
