using Klacks.Api.Presentation.Resources.Filter;
using MediatR;

namespace Klacks.Api.Queries.Clients;

public record LastChangeMetaDataQuery : IRequest<LastChangeMetaDataResource>;
