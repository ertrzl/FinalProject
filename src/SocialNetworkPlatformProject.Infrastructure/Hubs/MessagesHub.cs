using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SocialNetworkPlatformProject.Application.DTOs.Messages;
using SocialNetworkPlatformProject.Application.Exceptions.Base;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Infrastructure.Services;

namespace SocialNetworkPlatformProject.Infrastructure.Hubs;

// Clients call SendMessage / MarkAsRead / Typing; the other participant receives "ReceiveMessage", "MessagesRead" and
// "UserTyping" via SignalRNotifier. Every page keeps this connection open, so it doubles as the user's online presence.
[Authorize]
public class MessagesHub : Hub
{
    // Page navigations drop and re-open the connection within moments; only announce "offline" if it stays gone.
    private static readonly TimeSpan OfflineGracePeriod = TimeSpan.FromSeconds(4);

    private readonly IMessageService _messages;
    private readonly IUserService _users;
    private readonly ILiveUpdateService _live;
    private readonly PresenceTracker _presence;
    private readonly IValidator<PostMessageDto> _validator;

    public MessagesHub(
        IMessageService messages,
        IUserService users,
        ILiveUpdateService live,
        PresenceTracker presence,
        IValidator<PostMessageDto> validator)
    {
        _messages = messages;
        _users = users;
        _live = live;
        _presence = presence;
        _validator = validator;
    }

    public async Task<GetMessageDto> SendMessage(PostMessageDto dto)
    {
        var validation = await _validator.ValidateAsync(dto);
        if (!validation.IsValid)
            throw new HubException(string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        try
        {
            return await _messages.SendAsync(CurrentUserId, dto);
        }
        catch (BaseException ex)
        {
            throw new HubException(ex.Message);
        }
    }

    public async Task MarkAsRead(Guid conversationId)
    {
        try
        {
            await _messages.MarkConversationAsReadAsync(CurrentUserId, conversationId);
        }
        catch (BaseException ex)
        {
            throw new HubException(ex.Message);
        }
    }

    public async Task Typing(Guid conversationId)
    {
        try
        {
            await _messages.NotifyTypingAsync(CurrentUserId, conversationId);
        }
        catch (BaseException ex)
        {
            throw new HubException(ex.Message);
        }
    }

    public override async Task OnConnectedAsync()
    {
        await _users.UpdateLastSeenAsync(CurrentUserId);

        if (_presence.Connected(CurrentUserId, Context.ConnectionId))
            await _live.PresenceChangedAsync(CurrentUserId);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = CurrentUserId;
        await _users.UpdateLastSeenAsync(userId);

        if (_presence.Disconnected(userId, Context.ConnectionId))
        {
            await Task.Delay(OfflineGracePeriod);
            if (!_presence.IsOnline(userId))
                await _live.PresenceChangedAsync(userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    private Guid CurrentUserId => Guid.Parse(Context.UserIdentifier!);
}
