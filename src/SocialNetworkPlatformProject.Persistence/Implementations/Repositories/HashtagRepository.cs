using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Persistence.Contexts;
using SocialNetworkPlatformProject.Persistence.Implementations.Repositories.Generic;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Repositories;

public class HashtagRepository : Repository<Hashtag>, IHashtagRepository
{
    public HashtagRepository(ApplicationDbContext context) : base(context)
    {
    }
}
