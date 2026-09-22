namespace MediX;

/// <summary>
/// Marca um request cuja execução deve ser serializada por chave de concorrência.
/// Convenção da chave: "{Entidade}:{Id}".
/// </summary>
public interface IConcurrencyScoped
{
    string ConcurrencyKey { get; }
}
