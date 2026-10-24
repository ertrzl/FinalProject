using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Persistence.Contexts;
using SocialNetworkPlatformProject.Persistence.Implementations.Repositories.Generic;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Repositories;

public class MessageRepository : Repository<Message>, IMessageRepository
{
    public MessageRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Dictionary<Guid, int>> CountGroupUnreadAsync(
        Guid userId, IReadOnlyCollection<Guid> conversationIds, IReadOnlyCollection<Guid> hiddenSenderIds)
    {
        if (conversationIds.Count == 0)
            return new Dictionary<Guid, int>();

        var query =
            from m in _dbSet.AsNoTracking()
            join p in _context.Set<ConversationParticipant>().AsNoTracking() on m.ConversationId equals p.ConversationId
            where p.UserId == userId
                  && conversationIds.Contains(m.ConversationId)
                  && m.SenderId != userId
                  && m.DeletedAt == null
                  && !hiddenSenderIds.Contains(m.SenderId)
                  && m.CreatedAt > (p.LastReadAt ?? p.CreatedAt)
            group m by m.ConversationId into g
            select new { ConversationId = g.Key, Count = g.Count() };

        return await query.ToDictionaryAsync(x => x.ConversationId, x => x.Count);
    }
}
