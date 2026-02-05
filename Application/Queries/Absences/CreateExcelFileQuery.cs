using Klacks.Api.Application.DTOs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Absences
{
    public record CreateExcelFileQuery(string Language) : IRequest<HttpResultResource>;
}
