using Klacks.Api.Presentation.DTOs.Registrations;
using MediatR;

namespace Klacks.Api.Application.Queries.Accounts;

public record RefreshTokenQuery(RefreshRequestResource RefreshRequest) : IRequest<TokenResource?>;