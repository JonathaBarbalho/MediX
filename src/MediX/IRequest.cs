namespace MediX;

/// <summary>
/// Marcador comum para uma requisição despachada via <see cref="IMediator"/>, que produz uma
/// resposta do tipo <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TResponse">Tipo da resposta produzida pelo handler correspondente.</typeparam>
public interface IRequest<TResponse>;
