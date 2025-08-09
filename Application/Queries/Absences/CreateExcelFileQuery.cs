using Klacks.Api.Presentation.DTOs;
using MediatR;

namespace Klacks.Api.Application.Queries.Absences
{
    public record CreateExcelFileQuery(string Language) : IRequest<HttpResultResource>;
}
