namespace MediX;

/// <summary>
/// Requisição que apenas lê estado (read), sem efeitos colaterais. Marcador especializado de
/// <see cref="IRequest{TResponse}"/>.
/// </summary>
/// <typeparam name="TResponse">Tipo da resposta produzida pelo handler correspondente.</typeparam>
public interface IQuery<TResponse> : IRequest<TResponse>;
