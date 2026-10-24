using Microsoft.Net.Http.Headers;
using SocialNetworkPlatformProject.Application.Interfaces.Services;

namespace SocialNetworkPlatformProject.Extensions;

public class HttpClientInfo : IClientInfo
{
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
