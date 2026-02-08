using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Absences;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Application.DTOs.Filter;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Absences
{
    public class GetTruncatedListQueryHandler : BaseHandler, IRequestHandler<TruncatedListQuery, TruncatedAbsence>
    {
        private readonly IAbsenceRepository _absenceRepository;

        public GetTruncatedListQueryHandler(
            IAbsenceRepository absenceRepository,
            ILogger<GetTruncatedListQueryHandler> logger)
            : base(logger)
        {
            _absenceRepository = absenceRepository;
        }

        public async Task<TruncatedAbsence> Handle(TruncatedListQuery request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(async () =>
            {
                if (request.Filter == null)
                {
                    throw new InvalidRequestException("Filter parameter is required for truncated list query");
                }

                var result = await _absenceRepository.Truncated(request.Filter);

                if (result == null)
                {
                    throw new KeyNotFoundException("No absence data found for the specified filter criteria");
                }

                return result;
            }, nameof(Handle));
        }
    }
}
