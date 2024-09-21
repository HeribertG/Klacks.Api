using MediatR;

namespace Klacks.Api.Commands;

public record PostCommand<TModel>(TModel Resource) : IRequest<TModel?>;
