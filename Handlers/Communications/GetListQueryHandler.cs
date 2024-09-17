using AutoMapper;
using Klacks_api.Interfaces;
using Klacks_api.Queries;
using Klacks_api.Resources.Settings;
using MediatR;

namespace Klacks_api.Handlers.Communications
{
  public class GetListQueryHandler : IRequestHandler<ListQuery<CommunicationResource>, IEnumerable<CommunicationResource>>
  {
    private readonly IMapper mapper;
    private readonly ICommunicationRepository repository;

    public GetListQueryHandler(IMapper mapper,
                               ICommunicationRepository repository)
    {
      this.mapper = mapper;
      this.repository = repository;
    }

    public async Task<IEnumerable<CommunicationResource>> Handle(ListQuery<CommunicationResource> request, CancellationToken cancellationToken)
    {
      var communications = await repository.List();

      return mapper.Map<List<Models.Staffs.Communication>, List<CommunicationResource>>(communications!);
    }
  }
}
