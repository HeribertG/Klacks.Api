using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands;

public record DeleteCommand<TModel>(Guid Id) : IRequest<TModel?>;
