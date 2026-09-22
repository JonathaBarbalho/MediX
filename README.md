# MediX

Mediador CQRS leve para .NET — implementação caseira, sem dependência do MediatR, com resolução
de handlers via `IServiceProvider` e suporte a pipeline behaviors.

## Abstrações

- `IRequest<TResponse>` — marcador comum para comandos e queries.
- `ICommand<TResponse>` — requisição que muta estado.
- `IQuery<TResponse>` — requisição que apenas lê estado.
- `IRequestHandler<TRequest, TResponse>` — `Task<TResponse> HandleAsync(TRequest, CancellationToken)`.
- `IMediator` — `Task<TResponse> SendAsync<TResponse>(IRequest<TResponse>, CancellationToken)`.
- `IPipelineBehavior<TRequest, TResponse>` — intercepta o envio de um request antes/depois do handler.

## Padrão de uso

Cada caso de uso define um `record` que implementa `ICommand<T>` ou `IQuery<T>` e um handler
que implementa `IRequestHandler<TRequest, TResponse>`.

```csharp
public sealed record GetThingByIdQuery(Guid Id) : IQuery<ThingDto>;

internal sealed class GetThingByIdQueryHandler(IThingRepository things)
    : IRequestHandler<GetThingByIdQuery, ThingDto>
{
    public async Task<ThingDto> HandleAsync(GetThingByIdQuery query, CancellationToken cancellationToken)
    {
        var thing = await things.GetByIdAsync(query.Id, cancellationToken);
        return thing.ToDto();
    }
}
```

## Registro

Chame `AddMediX` informando os assemblies que contêm os handlers. O método registra o
`IMediator` e descobre/registra automaticamente todos os `IRequestHandler<,>` (incluindo tipos
`internal`) por assembly scanning.

```csharp
services.AddMediX(typeof(DependencyInjection).Assembly);
```

Em seguida, injete `IMediator` e despache requisições:

```csharp
var dto = await mediator.SendAsync(new GetThingByIdQuery(id), cancellationToken);
```

## Serialização por chave de concorrência (opcional)

Um request pode implementar `IConcurrencyScoped` para ter sua execução serializada por chave —
duas requisições com a mesma chave nunca executam concorrentemente; chaves diferentes executam em
paralelo.

```csharp
public sealed record UpdateStockCommand(Guid ProductId, int Quantity)
    : ICommand<Unit>, IConcurrencyScoped
{
    public string ConcurrencyKey => $"Product:{ProductId}";
}
```

Habilite o pipeline behavior correspondente:

```csharp
services.AddConcurrencySerialization();
```

## Escopo atual

MediX cobre hoje apenas o padrão request/response (comandos e queries). Não há suporte a
notificações/pub-sub (`INotification`/`INotificationHandler`, como no MediatR).
