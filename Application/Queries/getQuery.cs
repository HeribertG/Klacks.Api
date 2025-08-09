using MediatR;

namespace Klacks.Api.Application.Queries;

public record GetQuery<TModel>(Guid Id) : IRequest<TModel>;
