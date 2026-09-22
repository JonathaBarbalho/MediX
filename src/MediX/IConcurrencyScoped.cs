namespace MediX;

/// <summary>
/// Marca um request cuja execução deve ser serializada por chave de concorrência.
/// Convenção da chave: "{Entidade}:{Id}".
/// </summary>
public interface IConcurrencyScoped
{
    /// <summary>Chave que identifica o escopo de serialização. Convenção: "{Entidade}:{Id}".</summary>
    string ConcurrencyKey { get; }
}
