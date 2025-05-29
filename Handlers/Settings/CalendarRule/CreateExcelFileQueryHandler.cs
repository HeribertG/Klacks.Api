using Klacks.Api.Interfaces;
using Klacks.Api.Queries.Settings.CalendarRules;
using Klacks.Api.Resources;
using MediatR;

namespace Klacks.Api.Handlers.Settings.CalendarRule
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
