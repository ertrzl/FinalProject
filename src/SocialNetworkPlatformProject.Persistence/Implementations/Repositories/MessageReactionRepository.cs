using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Persistence.Contexts;
using SocialNetworkPlatformProject.Persistence.Implementations.Repositories.Generic;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Repositories;

public class MessageReactionRepository : Repository<MessageReaction>, IMessageReactionRepository
{
    public MessageReactionRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<MessageReaction>> GetForMessagesAsync(IReadOnlyCollection<Guid> messageIds)
    {
        if (messageIds.Count == 0)
            return new List<MessageReaction>();

        return await _dbSet.AsNoTracking().Where(r => messageIds.Contains(r.MessageId)).ToListAsync();
    }

    public async Task DeleteForMessageAsync(Guid messageId)
    {
        await _dbSet.Where(r => r.MessageId == messageId).ExecuteDeleteAsync();
    }
}
