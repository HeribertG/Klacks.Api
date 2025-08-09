using MediatR;

namespace Klacks.Api.Queries;

public record ListQuery<TModel>() : IRequest<IEnumerable<TModel>>;
