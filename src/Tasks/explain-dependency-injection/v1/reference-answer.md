A strong answer covers the following.

**What it is.** A class receives the objects it depends on from the outside, usually through its constructor, instead of creating them itself. It is a form of inversion of control: the class states what it needs (often as an interface) and a composition root or a container decides which implementation it gets.

```csharp
public sealed class OrderService(IOrderRepository orders, TimeProvider time)
{
    public Order Place(Cart cart) => orders.Add(Order.From(cart, time.GetUtcNow()));
}
```

**Benefits.**

- Testability: tests pass fakes or stubs instead of a real database or clock.
- Loose coupling: the class depends on an abstraction, so an implementation can change without touching it.
- Configuration in one place: the composition root wires the application, including cross-cutting concerns such as logging or caching decorators.
- Explicit dependencies: the constructor shows what a class needs.

**Costs.** More indirection, so it is harder to follow which implementation runs; wiring errors may only show at runtime; a long constructor can hide a class with too many responsibilities.

**Lifetimes in ASP.NET Core.**

- *Transient*: a new instance every time it is resolved. Fits lightweight, stateless services.
- *Scoped*: one instance per scope, which in ASP.NET Core is one HTTP request. Fits a `DbContext` or a unit of work.
- *Singleton*: one instance for the whole application. Fits expensive, thread-safe, stateless services such as a cache or a `TimeProvider`.

**Pitfall.** A singleton must not depend on a scoped service (a captive dependency): the scoped instance would live as long as the application, for example a `DbContext` shared across requests. The container's scope validation catches this in Development.
