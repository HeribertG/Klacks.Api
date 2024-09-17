using Klacks_api.Resources.Filter;
using MediatR;

namespace Klacks_api.Queries.Clients;

public record LastChangeMetaDataQuery : IRequest<LastChangeMetaDataResource>;
