using MediatR;

namespace Klacks.Api.Commands;

public record PutCommand<TModel>(TModel Resource) : IRequest<TModel?>;
