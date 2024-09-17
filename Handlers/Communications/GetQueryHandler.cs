using AutoMapper;
using Klacks_api.Interfaces;
using Klacks_api.Queries;
using Klacks_api.Resources.Settings;
using MediatR;

namespace Klacks_api.Handlers.Communications
{
  public class GetQueryHandler : IRequestHandler<GetQuery<CommunicationResource>, CommunicationResource>
  {
    private readonly IMapper mapper;
    private readonly ICommunicationRepository repository;

    public GetQueryHandler(IMapper mapper,
                           ICommunicationRepository repository)
    {
      this.mapper = mapper;
      this.repository = repository;
    }

    public async Task<CommunicationResource> Handle(GetQuery<CommunicationResource> request, CancellationToken cancellationToken)
    {
      var communication = await repository.Get(request.Id);
      return mapper.Map<Models.Staffs.Communication, CommunicationResource>(communication!);
    }
  }
}
