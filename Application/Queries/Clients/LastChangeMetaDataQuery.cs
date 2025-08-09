using Klacks.Api.Presentation.DTOs.Filter;
using MediatR;

namespace Klacks.Api.Application.Queries.Clients;

public record LastChangeMetaDataQuery : IRequest<LastChangeMetaDataResource>;
