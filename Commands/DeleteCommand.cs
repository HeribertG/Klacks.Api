using MediatR;

namespace Klacks_api.Commands;

public record DeleteCommand<TModel>(Guid Id) : IRequest<TModel?>;
