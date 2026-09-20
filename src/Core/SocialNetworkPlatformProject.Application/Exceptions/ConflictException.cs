using SocialNetworkPlatformProject.Application.Exceptions.Base;

namespace SocialNetworkPlatformProject.Application.Exceptions;

public class ConflictException : BaseException
{
    public ConflictException(string message) : base(message, 409) { }
}
