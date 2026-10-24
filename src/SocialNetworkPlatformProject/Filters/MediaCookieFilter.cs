using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SocialNetworkPlatformProject.Application.DTOs.Users;
using SocialNetworkPlatformProject.Extensions;

namespace SocialNetworkPlatformProject.Filters;

public class MediaCookieFilter : IResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is ObjectResult { Value: TokenResponseDto { MediaToken: not null, MediaExpiresAt: not null } session })
            context.HttpContext.AppendMediaCookie(session.MediaToken, session.MediaExpiresAt.Value);
    }

    public void OnResultExecuted(ResultExecutedContext context)
    {
    }
}
