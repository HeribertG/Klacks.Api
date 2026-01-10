using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.OAuth2;
using Klacks.Api.Presentation.DTOs.Registrations;

namespace Klacks.Api.Application.Commands.OAuth2;

public record OAuth2CallbackCommand(OAuth2CallbackRequest Request) : IRequest<TokenResource>;
