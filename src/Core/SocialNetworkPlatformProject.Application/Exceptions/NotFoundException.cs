using SocialNetworkPlatformProject.Application.Exceptions.Base;

namespace SocialNetworkPlatformProject.Application.Exceptions;

public class NotFoundException : BaseException
{
    public NotFoundException(string message) : base(message, 404) { }
}
