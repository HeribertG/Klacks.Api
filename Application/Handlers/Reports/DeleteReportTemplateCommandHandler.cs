// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Reports;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Reports;

public class DeleteReportTemplateCommandHandler : IRequestHandler<DeleteReportTemplateCommand>
{
    private readonly IReportTemplateRepository _templateRepository;

    public DeleteReportTemplateCommandHandler(IReportTemplateRepository templateRepository)
    {
        _templateRepository = templateRepository;
    }

    public async Task<Unit> Handle(DeleteReportTemplateCommand request, CancellationToken cancellationToken)
    {
        await _templateRepository.DeleteAsync(request.Id, cancellationToken);
        return Unit.Value;
    }
}
