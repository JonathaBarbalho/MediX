namespace MediX;

/// <summary>
/// Requisição que muta estado (write). Marcador especializado de <see cref="IRequest{TResponse}"/>.
/// </summary>
/// <typeparam name="TResponse">Tipo da resposta produzida pelo handler correspondente.</typeparam>
public interface ICommand<TResponse> : IRequest<TResponse>;
