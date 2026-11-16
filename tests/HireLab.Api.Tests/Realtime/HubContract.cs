using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.SignalR;

namespace HireLab.Api.Tests.Realtime;

/// <summary>
/// Describes a typed hub as the JSON contract the web workspace commits in <c>projects/api-client/hubs</c> and
/// generates its TypeScript from (ADR 0031): the methods clients invoke, the messages the hub sends and the records
/// they carry, with their JSON types.
/// </summary>
internal static class HubContract
{
    private static readonly string[] _lifetimeMethods = [nameof(Hub.OnConnectedAsync), nameof(Hub.OnDisconnectedAsync)];

    public static JsonObject Describe(Type hub, Type client, string path)
    {
        var records = new SortedDictionary<string, Type>(StringComparer.Ordinal);

        var serverMethods = new JsonObject();
        foreach (var method in hub.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                     .Where(method => !_lifetimeMethods.Contains(method.Name))
                     .OrderBy(method => method.MetadataToken))
        {
            serverMethods[method.Name] = new JsonObject
            {
                ["parameters"] = Parameters(method, records),
                ["returns"] = JsonTypeOf(ResultTypeOf(method.ReturnType), records),
            };
        }

        var clientMethods = new JsonObject();
        foreach (var method in client.GetMethods().OrderBy(method => method.MetadataToken))
        {
            clientMethods[method.Name] = Parameters(method, records);
        }

        var described = new SortedDictionary<string, JsonObject>(StringComparer.Ordinal);
        while (records.Keys.FirstOrDefault(name => !described.ContainsKey(name)) is { } name)
        {
            var properties = new JsonObject();
            foreach (var property in records[name].GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                properties[JsonNamingPolicy.CamelCase.ConvertName(property.Name)] = JsonTypeOf(property.PropertyType, records);
            }

            described[name] = properties;
        }

        var types = new JsonObject();
        foreach (var (name, properties) in described)
        {
            types[name] = properties;
        }

        return new JsonObject
        {
            ["path"] = path,
            ["serverMethods"] = serverMethods,
            ["clientMethods"] = clientMethods,
            ["types"] = types,
        };
    }

    private static JsonObject Parameters(MethodInfo method, IDictionary<string, Type> records)
    {
        var parameters = new JsonObject();
        foreach (var parameter in method.GetParameters())
        {
            parameters[parameter.Name!] = JsonTypeOf(parameter.ParameterType, records);
        }

        return parameters;
    }

    private static Type? ResultTypeOf(Type returnType) =>
        returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>) ? returnType.GetGenericArguments()[0]
        : returnType == typeof(Task) ? null
        : throw new InvalidOperationException($"Hub methods return Task or Task<T>, not {returnType.Name}.");

    private static string JsonTypeOf(Type? type, IDictionary<string, Type> records)
    {
        if (type is null)
        {
            return "void";
        }

        if (Nullable.GetUnderlyingType(type) is { } underlying)
        {
            return $"{JsonTypeOf(underlying, records)} | null";
        }

        if (type == typeof(Guid) || type == typeof(string) || type == typeof(DateTimeOffset))
        {
            return "string";
        }

        if (type == typeof(int) || type == typeof(long) || type == typeof(double))
        {
            return "number";
        }

        if (type == typeof(bool))
        {
            return "boolean";
        }

        if (type.IsClass && type.Namespace?.StartsWith("HireLab.", StringComparison.Ordinal) == true)
        {
            records[type.Name] = type;
            return type.Name;
        }

        throw new InvalidOperationException($"{type.Name} has no JSON type in the hub contract yet; add it to {nameof(HubContract)}.");
    }
}
