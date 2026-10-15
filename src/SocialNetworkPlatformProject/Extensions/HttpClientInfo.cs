using Microsoft.Net.Http.Headers;
using SocialNetworkPlatformProject.Application.Interfaces.Services;

namespace SocialNetworkPlatformProject.Extensions;

// The browser and the address of the current request, for the services that must not know about HttpContext.
public class HttpClientInfo : IClientInfo
{
    // Longer than any real browser string needs; the session list only uses it to name the browser and the system.
    private const int MaxUserAgentLength = 512;

    private readonly IHttpContextAccessor _accessor;

    public HttpClientInfo(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public string? UserAgent
    {
        get
        {
            var value = _accessor.HttpContext?.Request.Headers[HeaderNames.UserAgent].ToString();
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return value.Length <= MaxUserAgentLength ? value : value[..MaxUserAgentLength];
        }
    }

    public string? IpAddress => _accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}
