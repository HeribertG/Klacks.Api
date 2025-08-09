using MediatR;

namespace Klacks.Api.Application.Commands;

public record DeleteCommand<TModel>(Guid Id) : IRequest<TModel?>;
