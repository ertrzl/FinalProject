using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SocialNetworkPlatformProject.Application.DTOs.Users;
using SocialNetworkPlatformProject.Extensions;

namespace SocialNetworkPlatformProject.Filters;

public class MediaCookieFilter : IResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        var session = context.Result is ObjectResult { Value: var value }
            ? value as TokenResponseDto ?? (value as RegisterResultDto)?.Session
            : null;

        if (session is { MediaToken: not null, MediaExpiresAt: not null })
            context.HttpContext.AppendMediaCookie(session.MediaToken, session.MediaExpiresAt.Value);
    }

    public void OnResultExecuted(ResultExecutedContext context)
    {
    }
}
