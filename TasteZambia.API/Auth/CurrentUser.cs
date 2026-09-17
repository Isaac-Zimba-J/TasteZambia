using System.Security.Claims;

namespace TasteZambia.API.Auth;

public interface ICurrentUser
{
    string? UserId { get; }
    bool IsAuthenticated { get; }
}

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public string? UserId => accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? accessor.HttpContext?.User.FindFirstValue("sub");

    public bool IsAuthenticated => UserId is not null;
}
