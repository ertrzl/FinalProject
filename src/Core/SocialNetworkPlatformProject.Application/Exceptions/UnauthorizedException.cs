using SocialNetworkPlatformProject.Application.Exceptions.Base;

namespace SocialNetworkPlatformProject.Application.Exceptions;

public class UnauthorizedException : BaseException
{
    public UnauthorizedException(string message) : base(message, 401) { }
}
