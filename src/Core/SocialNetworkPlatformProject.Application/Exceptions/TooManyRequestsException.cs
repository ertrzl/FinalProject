using SocialNetworkPlatformProject.Application.Exceptions.Base;

namespace SocialNetworkPlatformProject.Application.Exceptions;

public class TooManyRequestsException : BaseException
{
    public TooManyRequestsException(string message) : base(message, 429) { }
}
