using Klacks.Api.Presentation.DTOs.Registrations;
using MediatR;

namespace Klacks.Api.Application.Queries.Accounts;

public record LoginUserQuery(string Email, string Password) : IRequest<TokenResource>;