using MediatR;

namespace Klacks.Api.Application.Commands;

public record PostCommand<TModel>(TModel Resource) : IRequest<TModel?>;
