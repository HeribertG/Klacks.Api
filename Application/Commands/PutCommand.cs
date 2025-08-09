using MediatR;

namespace Klacks.Api.Application.Commands;

public record PutCommand<TModel>(TModel Resource) : IRequest<TModel?>;
