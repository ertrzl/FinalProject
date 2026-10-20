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

    public async Task<Guid?> FindGroupAsync(Guid groupId)
    {
        return await _dbSet.AsNoTracking()
            .Where(c => c.GroupId == groupId)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<Guid> GetOrCreateGroupAsync(Guid groupId)
    {
        var existing = await FindGroupAsync(groupId);
        if (existing.HasValue)
            return existing.Value;

        var memberIds = await _context.Set<GroupMember>().AsNoTracking()
            .Where(m => m.GroupId == groupId)
            .Select(m => m.UserId)
            .ToListAsync();

        var conversation = new Conversation { GroupId = groupId };
        foreach (var memberId in memberIds)
            conversation.Participants.Add(new ConversationParticipant { UserId = memberId });
        await _dbSet.AddAsync(conversation);

        try
        {
            await _context.SaveChangesAsync();
            return conversation.Id;
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            // Another member opened it a moment ago: drop ours and use theirs.
            _context.Entry(conversation).State = EntityState.Detached;
            foreach (var participant in conversation.Participants)
                _context.Entry(participant).State = EntityState.Detached;

            var winner = await FindGroupAsync(groupId);
            if (winner == null)
                throw;

            return winner.Value;
        }
    }

    public async Task SyncGroupChatsAsync(Guid userId)
    {
        var myGroupIds = await _context.Set<GroupMember>().AsNoTracking()
            .Where(m => m.UserId == userId)
            .Select(m => m.GroupId)
            .ToListAsync();

        var chats = await _dbSet.AsNoTracking()
            .Where(c => c.GroupId != null && myGroupIds.Contains(c.GroupId.Value))
            .Select(c => new { c.Id, GroupId = c.GroupId!.Value })
            .ToListAsync();

        // A group without a chat yet (made before group chats existed, or never opened): created with all its members.
        foreach (var groupId in myGroupIds.Except(chats.Select(c => c.GroupId)))
            await GetOrCreateGroupAsync(groupId);

        var participants = _context.Set<ConversationParticipant>();

        var chatIds = chats.Select(c => c.Id).ToList();
        var joinedChatIds = await participants.AsNoTracking()
            .Where(p => p.UserId == userId && chatIds.Contains(p.ConversationId))
            .Select(p => p.ConversationId)
            .ToListAsync();
        var missing = chatIds.Except(joinedChatIds).ToList();
        foreach (var chatId in missing)
            await participants.AddAsync(new ConversationParticipant { ConversationId = chatId, UserId = userId });

        if (missing.Count > 0)
        {
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.IsUniqueViolation())
            {
                // GroupService added the same row at the same moment: it is there either way.
                foreach (var entry in _context.ChangeTracker.Entries<ConversationParticipant>().Where(e => e.State == EntityState.Added).ToList())
                    entry.State = EntityState.Detached;
            }
        }

        // Still a participant of the chat of a group they are no longer in: out.
        await participants
            .Where(p => p.UserId == userId
                && _dbSet.Any(c => c.Id == p.ConversationId && c.GroupId != null && !myGroupIds.Contains(c.GroupId.Value)))
            .ExecuteDeleteAsync();
    }

    public async Task<Dictionary<Guid, Guid>> GetGroupIdsAsync(IReadOnlyCollection<Guid> conversationIds)
    {
        if (conversationIds.Count == 0)
            return new Dictionary<Guid, Guid>();

        return await _dbSet.AsNoTracking()
            .Where(c => conversationIds.Contains(c.Id) && c.GroupId != null)
            .ToDictionaryAsync(c => c.Id, c => c.GroupId!.Value);
    }

    public async Task<HashSet<Guid>> GetGroupChatIdsAsync(IReadOnlyCollection<Guid> conversationIds)
    {
        if (conversationIds.Count == 0)
            return new HashSet<Guid>();

        var ids = await _dbSet.AsNoTracking()
            .Where(c => conversationIds.Contains(c.Id) && (c.GroupId != null || c.Title != null))
            .Select(c => c.Id)
            .ToListAsync();

        return ids.ToHashSet();
    }

    public async Task<List<string>> GetMediaUrlsAsync(Guid conversationId)
    {
        return await _context.Set<Message>().AsNoTracking()
            .Where(m => m.ConversationId == conversationId && m.MediaUrl != null)
            .Select(m => m.MediaUrl!)
            .ToListAsync();
    }

    public async Task<List<string>> GetGroupChatMediaUrlsAsync(Guid groupId)
    {
        return await _context.Set<Message>().AsNoTracking()
            .Where(m => m.Conversation!.GroupId == groupId && m.MediaUrl != null)
            .Select(m => m.MediaUrl!)
            .ToListAsync();
    }
}
