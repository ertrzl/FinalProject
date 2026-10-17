namespace SocialNetworkPlatformProject.Middlewares;

// Pages are served without ".html": /friends shows wwwroot/friends.html, and the old /friends.html (a bookmark, a link in a
// mail or a notification) is sent to /friends with its query string kept (event.html?id=5 -> /event?id=5). Only top-level
// pages that exist as a file take part; 404.html is left alone (it is the error page, not an address).
public class CleanUrlMiddleware
{
    private const string Extension = ".html";
    private const string NotFoundPage = "/404";

    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _environment;

    public CleanUrlMiddleware(RequestDelegate next, IWebHostEnvironment environment)
    {
        _next = next;
        _environment = environment;
    }

    public Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;
        if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method)) return _next(context);

        var path = request.Path.Value ?? "/";
        if (path.Length > 1 && path.IndexOf('/', 1) == -1)
        {
            if (path.EndsWith(Extension, StringComparison.OrdinalIgnoreCase))
            {
                var clean = path[..^Extension.Length];
                if (!IsNotFoundPage(clean) && PageExists(path))
                {
                    var target = clean.Equals("/index", StringComparison.OrdinalIgnoreCase) ? "/" : clean;
                    context.Response.Redirect(request.PathBase + target + request.QueryString, permanent: true);
                    return Task.CompletedTask;
                }
            }
            else if (!Path.HasExtension(path) && !IsNotFoundPage(path) && PageExists(path + Extension))
            {
                request.Path = path + Extension;
            }
        }

        return _next(context);
    }

    private static bool IsNotFoundPage(string path) => path.Equals(NotFoundPage, StringComparison.OrdinalIgnoreCase);

    private bool PageExists(string path) => _environment.WebRootFileProvider.GetFileInfo(path).Exists;
}
