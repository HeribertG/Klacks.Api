using MediatR;

namespace Klacks.Api.Commands;

public record DeleteCommand<TModel>(Guid Id) : IRequest<TModel?>;
