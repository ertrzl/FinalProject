using SocialNetworkPlatformProject.Application.Exceptions.Base;

namespace SocialNetworkPlatformProject.Application.Exceptions;

public class BadRequestException : BaseException
{
    public BadRequestException(string message) : base(message, 400) { }
}
