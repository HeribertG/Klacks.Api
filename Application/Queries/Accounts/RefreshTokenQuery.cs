using Klacks.Api.Application.DTOs.Registrations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Accounts;

public record RefreshTokenQuery(RefreshRequestResource RefreshRequest) : IRequest<TokenResource?>;