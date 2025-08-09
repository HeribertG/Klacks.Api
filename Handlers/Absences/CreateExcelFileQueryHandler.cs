using Klacks.Api.Interfaces;
using Klacks.Api.Application.Queries.Absences;
using Klacks.Api.Presentation.DTOs;
using MediatR;

namespace Klacks.Api.Handlers.Absences;

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
