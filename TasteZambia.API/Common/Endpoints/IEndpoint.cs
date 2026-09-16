namespace TasteZambia.API.Common.Endpoints;

/// <summary>One HTTP endpoint. Implementations are discovered by assembly scan.</summary>
public interface IEndpoint
{
    static abstract void Map(IEndpointRouteBuilder app);
}
