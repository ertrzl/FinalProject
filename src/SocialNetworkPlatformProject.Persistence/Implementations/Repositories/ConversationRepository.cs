using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Persistence.Contexts;
using SocialNetworkPlatformProject.Persistence.Extensions;
using SocialNetworkPlatformProject.Persistence.Implementations.Repositories.Generic;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Repositories;

public class ConversationRepository : Repository<Conversation>, IConversationRepository
{
    public ConversationRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Guid?> FindDirectAsync(Guid userA, Guid userB)
    {
        var key = Conversation.DirectKeyFor(userA, userB);

        return await _dbSet.AsNoTracking()
            .Where(c => c.DirectKey == key)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<Guid> GetOrCreateDirectAsync(Guid userA, Guid userB)
    {
        var existing = await FindDirectAsync(userA, userB);
        if (existing.HasValue)
            return existing.Value;

        var conversation = new Conversation { DirectKey = Conversation.DirectKeyFor(userA, userB) };
        conversation.Participants.Add(new ConversationParticipant { UserId = userA });
        conversation.Participants.Add(new ConversationParticipant { UserId = userB });
        await _dbSet.AddAsync(conversation);

        try
        {
            await _context.SaveChangesAsync();
            return conversation.Id;
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            // The other person's request created it a moment ago: drop ours and use theirs.
            _context.Entry(conversation).State = EntityState.Detached;
            foreach (var participant in conversation.Participants)
                _context.Entry(participant).State = EntityState.Detached;

            var winner = await FindDirectAsync(userA, userB);
            if (winner == null)
                throw;

            return winner.Value;
        }
    }
}
