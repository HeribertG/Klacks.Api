using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Accounts;

public record ValidatePasswordResetTokenQuery(string Token) : IRequest<bool>;