using System.Security.Claims;

namespace PaymentService.Utils;

/// <summary>
/// Service for accessing current user information from JWT token
/// </summary>
public interface ICurrentUserService
{
    string UserId { get; }
    string? UserName { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    ClaimsPrincipal? User { get; }
}

/// <summary>
/// Implementation of current user service
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string UserId => User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
                           User?.FindFirst("sub")?.Value ?? 
                           User?.FindFirst("user_id")?.Value ?? 
                           "anonymous";

    public string? UserName => User?.FindFirst(ClaimTypes.Name)?.Value ?? 
                              User?.FindFirst("name")?.Value;

    public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value ?? 
                           User?.FindFirst("email")?.Value;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;
}
