using Klacks.Api.Resources.Filter;
using MediatR;

namespace Klacks.Api.Queries.Clients;

public record LastChangeMetaDataQuery : IRequest<LastChangeMetaDataResource>;
