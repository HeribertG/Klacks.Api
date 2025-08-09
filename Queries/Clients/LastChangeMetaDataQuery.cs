using Klacks.Api.Presentation.DTOs.Filter;
using MediatR;

namespace Klacks.Api.Queries.Clients;

public record LastChangeMetaDataQuery : IRequest<LastChangeMetaDataResource>;
