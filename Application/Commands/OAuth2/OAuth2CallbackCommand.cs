using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.OAuth2;
using Klacks.Api.Application.DTOs.Registrations;

namespace Klacks.Api.Application.Commands.OAuth2;

public record OAuth2CallbackCommand(OAuth2CallbackRequest Request) : IRequest<TokenResource>;
