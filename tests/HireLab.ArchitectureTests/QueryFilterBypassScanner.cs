using Mono.Cecil;

namespace HireLab.ArchitectureTests;

internal static class QueryFilterBypassScanner
{
    private const string ExtensionsType = "Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions";
    private const string BypassMethod = "IgnoreQueryFilters";

    public static IReadOnlyList<QueryFilterBypass> FindIn(string assemblyPath)
    {
        using var module = ModuleDefinition.ReadModule(assemblyPath);

        return module.GetTypes()
            .SelectMany(type => type.Methods.Where(method => method.HasBody).Select(method => (Type: type, Method: method)))
            .Where(caller => caller.Method.Body.Instructions.Any(instruction =>
                instruction.Operand is MethodReference { Name: BypassMethod } called
                && called.DeclaringType.FullName == ExtensionsType))
            .Select(caller => new QueryFilterBypass(module.Assembly.Name.Name, OutermostTypeOf(caller.Type).FullName, caller.Method.Name))
            .Distinct()
            .ToList();
    }

    private static TypeDefinition OutermostTypeOf(TypeDefinition type)
    {
        while (type.DeclaringType is not null)
        {
            type = type.DeclaringType;
        }

        return type;
    }
}

internal sealed record QueryFilterBypass(string Assembly, string Type, string Method)
{
    public override string ToString() => $"{Assembly}: {Type}.{Method}";
}
