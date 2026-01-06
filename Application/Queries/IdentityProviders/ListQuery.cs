using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.IdentityProviders;

namespace Klacks.Api.Application.Queries.IdentityProviders;

public record ListQuery : IRequest<IEnumerable<IdentityProviderListResource>>;
