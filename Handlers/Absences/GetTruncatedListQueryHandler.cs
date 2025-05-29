using AutoMapper;
using Klacks.Api.Interfaces;
using Klacks.Api.Queries.Absences;
using Klacks.Api.Resources.Filter;
using MediatR;

namespace Klacks.Api.Handlers.Absences
{
    public class GetTruncatedListQueryHandler : IRequestHandler<TruncatedListQuery, TruncatedAbsence>
    {
        private readonly IMapper mapper;
        private readonly IAbsenceRepository repository;

        public GetTruncatedListQueryHandler(IMapper mapper,
                                            IAbsenceRepository repository)
        {
            this.mapper = mapper;
            this.repository = repository;
        }

        public async Task<TruncatedAbsence> Handle(TruncatedListQuery request, CancellationToken cancellationToken)
        {
            var tmp = await repository.Truncated(request.Filter);
            return mapper.Map<TruncatedAbsence_dto, TruncatedAbsence>(tmp!);
        }
    }
}
