namespace Klacks.Api.Infrastructure.Mediator;

public interface IRequest<out TResponse>;

public interface IRequest : IRequest<Unit>;
