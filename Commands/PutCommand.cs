using MediatR;

namespace Klacks_api.Commands;

public record PutCommand<TModel>(TModel Resource) : IRequest<TModel?>;
