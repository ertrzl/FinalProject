using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SocialNetworkPlatformProject.Application.DTOs.Users;
using SocialNetworkPlatformProject.Extensions;

namespace SocialNetworkPlatformProject.Filters;

// Every answer that carries a session (sign-in, registration, token renewal, a changed password) also sets the photo cookie
// (see MediaCookie), so the pictures of a page open for as long as the session lasts.
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
