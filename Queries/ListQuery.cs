using MediatR;

namespace Klacks_api.Queries;

public record ListQuery<TModel>() : IRequest<IEnumerable<TModel>>;
