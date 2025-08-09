using MediatR;

namespace Klacks.Api.Application.Queries;

public record ListQuery<TModel>() : IRequest<IEnumerable<TModel>>;
