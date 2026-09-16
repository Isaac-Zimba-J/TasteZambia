using System.Reflection;

namespace TasteZambia.API.Common.Endpoints;

public static class EndpointExtensions
{
    /// <summary>
    /// Finds every IEndpoint in this assembly and maps it. Adding a feature folder is
    /// therefore all it takes to add a route - no central registry to edit and forget.
    /// </summary>
    public static IEndpointRouteBuilder MapArchiveEndpoints(this IEndpointRouteBuilder app)
    {
        var endpoints = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false } && t.IsAssignableTo(typeof(IEndpoint)));

        foreach (var type in endpoints)
        {
            var map = type.GetMethod(nameof(IEndpoint.Map), BindingFlags.Public | BindingFlags.Static);
            map?.Invoke(null, [app]);
        }

        return app;
    }
}
