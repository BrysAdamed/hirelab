**If the candidate is stuck**

- Ask how they would unit test a class that creates its own `SqlConnection` inside a method.
- Ask what changes if the class asks for an `IOrderRepository` in its constructor instead.

**Follow-up questions**

- What happens if a singleton takes a scoped service in its constructor? How would you notice?
- When would you inject a factory or `IServiceScopeFactory` instead of the service itself?
- Is the service locator pattern (resolving from `IServiceProvider` inside a class) the same thing? Why do people warn against it?
- How would you replace a registration in an integration test?
