using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace UserManagementAPI.Middleware;

/// <summary>
/// Validates a static API token from the Authorization header (Bearer) or X-Api-Token.
/// Swagger and OpenAPI document routes are left open so the UI can load.
/// </summary>
public class TokenAuthenticationMiddleware
{
    public const string HeaderName = "X-Api-Token";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<TokenAuthenticationMiddleware> _logger;
    private readonly byte[] _expectedTokenBytes;

    public TokenAuthenticationMiddleware(
        RequestDelegate next,
        ILogger<TokenAuthenticationMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;

        var token = configuration["Authentication:ApiToken"]
            ?? throw new InvalidOperationException("Authentication:ApiToken is not configured.");
        _expectedTokenBytes = Encoding.UTF8.GetBytes(token);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsAnonymousPath(context.Request.Path))
        {
            await _next(context);
            return;
        }

        if (!TryGetToken(context.Request, out var providedToken) || !IsValidToken(providedToken))
        {
            _logger.LogWarning("Rejected {Method} {Path} with an invalid or missing token.",
                context.Request.Method, context.Request.Path);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            var payload = new { error = "Unauthorized. Provide a valid token in the Authorization or X-Api-Token header." };
            await context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
            return;
        }

        await _next(context);
    }

    private static bool IsAnonymousPath(PathString path)
    {
        return path.StartsWithSegments("/swagger")
            || path.StartsWithSegments("/openapi")
            || path == "/favicon.ico";
    }

    private static bool TryGetToken(HttpRequest request, out string token)
    {
        var authorization = request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(authorization)
            && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            token = authorization["Bearer ".Length..].Trim();
            return token.Length > 0;
        }

        token = request.Headers[HeaderName].ToString();
        return !string.IsNullOrWhiteSpace(token);
    }

    private bool IsValidToken(string providedToken)
    {
        var providedBytes = Encoding.UTF8.GetBytes(providedToken);
        if (providedBytes.Length != _expectedTokenBytes.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(providedBytes, _expectedTokenBytes);
    }
}
