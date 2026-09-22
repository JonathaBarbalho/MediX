# MediX

Mediador CQRS leve para .NET — implementação caseira, sem dependência do MediatR, com resolução
de handlers via `IServiceProvider` e suporte a pipeline behaviors.

## Instalação

```
dotnet add package MediX
```

## Abstrações

- `IRequest<TResponse>` — marcador comum para comandos e queries.
- `ICommand<TResponse>` — requisição que muta estado.
- `IQuery<TResponse>` — requisição que apenas lê estado.
- `IRequestHandler<TRequest, TResponse>` — `Task<TResponse> HandleAsync(TRequest, CancellationToken)`.
- `IMediator` — `SendAsync<TResponse>(IRequest<TResponse>, CancellationToken)` e
  `PublishAsync<TNotification>(TNotification, CancellationToken)`.
- `IPipelineBehavior<TRequest, TResponse>` — intercepta o envio de um request antes/depois do handler.
- `INotification` — marcador para uma notificação publicada via `PublishAsync`.
- `INotificationHandler<TNotification>` — `Task HandleAsync(TNotification, CancellationToken)`;
  vários podem existir para o mesmo tipo de notificação.
- `INotificationPublisher` — estratégia plugável de execução dos handlers de uma notificação.

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

## Notificações / pub-sub (opcional)

Além do request/response, o MediX suporta publish/subscribe: uma notificação pode ter zero, um ou
vários handlers, todos executados — sem retorno.

```csharp
public sealed record OrderCreatedNotification(Guid OrderId) : INotification;

internal sealed class SendConfirmationEmailHandler : INotificationHandler<OrderCreatedNotification>
{
    public Task HandleAsync(OrderCreatedNotification notification, CancellationToken cancellationToken)
    {
        // ...
        return Task.CompletedTask;
    }
}
```

Handlers (`INotificationHandler<TNotification>`) são descobertos pelo mesmo `AddMediX(...)` usado
para `IRequestHandler<,>`, incluindo tipos `internal`. Publique com:

```csharp
await mediator.PublishAsync(new OrderCreatedNotification(orderId), cancellationToken);
```

Notificações não passam pela pipeline de `IPipelineBehavior<,>` — isso é exclusivo de
request/response.

### Estratégia de execução dos handlers

Como os handlers de uma notificação são executados é definido por uma implementação de
`INotificationPublisher`, registrada via DI e trocável pelo consumidor. `AddMediX` já registra o
padrão (`SequentialStopOnFirstExceptionPublisher`); troque com um dos métodos de extensão:

| Método | Execução | Se um handler falhar |
|---|---|---|
| _(padrão, sem chamar nada)_ | Sequencial, na ordem de registro | Para imediatamente; handlers seguintes não rodam |
| `UseSequentialContinueOnExceptionNotificationPublisher()` | Sequencial, na ordem de registro | Todos rodam; falhas são agregadas numa `AggregateException` ao final |
| `UseParallelNotificationPublisher()` | Todos em paralelo | Todos rodam; falhas são agregadas numa `AggregateException` |

```csharp
services.AddMediX(typeof(DependencyInjection).Assembly);
services.UseParallelNotificationPublisher(); // opcional
```

Você também pode implementar `INotificationPublisher` do zero e registrar sua própria estratégia.

## Licença

MIT — veja [LICENSE](LICENSE).
