using MediatR;

namespace Klacks_api.Commands;

public record PostCommand<TModel>(TModel Resource) : IRequest<TModel?>;
