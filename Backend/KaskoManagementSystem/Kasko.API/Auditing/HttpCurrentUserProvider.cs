using System.Security.Claims;
using Kasko.DataAccess.Auditing;

namespace Kasko.API.Auditing;

public class HttpCurrentUserProvider : ICurrentUserProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentUserProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? GetCurrentUser()
    {
        var user = _httpContextAccessor.HttpContext?.User;

        if (user?.Identity?.IsAuthenticated != true)
        {
            return _httpContextAccessor.HttpContext == null ? "system" : "anonymous";
        }

        return user.FindFirst(ClaimTypes.Email)?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}
