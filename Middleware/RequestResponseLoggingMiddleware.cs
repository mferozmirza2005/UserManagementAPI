using System.Diagnostics;

namespace UserManagementAPI.Middleware;

/// <summary>
/// Logs HTTP method, request path, and response status code for auditing.
/// Registered last among the custom middleware so it records the final status after the endpoint runs.
/// </summary>
public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? "/";
        var statusCode = context.Response.StatusCode;

        try
        {
            await _next(context);
            statusCode = context.Response.StatusCode;
        }
        catch
        {
            statusCode = StatusCodes.Status500InternalServerError;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _logger.LogInformation(
                "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs} ms",
                method,
                path,
                statusCode,
                stopwatch.ElapsedMilliseconds);
        }
    }
}
