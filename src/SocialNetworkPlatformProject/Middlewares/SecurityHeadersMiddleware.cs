namespace SocialNetworkPlatformProject.Middlewares;

// Browser-side hardening that costs nothing: no content-type guessing, no framing by other sites, a referrer that
// does not leak page addresses, and a content security policy that only lets the page load what it actually uses
// (our own files plus the CDN and map-search hosts the pages reference).
// 'unsafe-inline' stays for scripts and styles because the pages use inline handlers (onclick="...") — dropping it
// would first need those moved into the .js files.
public class SecurityHeadersMiddleware
{
    private const string ContentSecurityPolicy =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
        "font-src 'self' data: https://cdn.jsdelivr.net; " +
        "img-src 'self' data: blob:; " +
        "media-src 'self' blob:; " +
        "connect-src 'self' ws: wss: https://nominatim.openstreetmap.org https://cdn.jsdelivr.net; " +
        "object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'";

    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        // OnStarting, not set-now: the error middleware clears the response (headers included) before writing its body.
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            headers["Content-Security-Policy"] = ContentSecurityPolicy;
            return Task.CompletedTask;
        });

        return _next(context);
    }
}
