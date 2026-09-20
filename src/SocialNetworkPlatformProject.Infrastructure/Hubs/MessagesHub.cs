using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SocialNetworkPlatformProject.Application.DTOs.Messages;
using SocialNetworkPlatformProject.Application.Exceptions.Base;
using SocialNetworkPlatformProject.Application.Interfaces.Services;

namespace SocialNetworkPlatformProject.Infrastructure.Hubs;

// Clients call SendMessage / MarkAsRead; the other participant receives "ReceiveMessage" via SignalRNotifier.
[Authorize]
public class MessagesHub : Hub
{
    private readonly IMessageService _messages;
    private readonly IUserService _users;
    private readonly IValidator<PostMessageDto> _validator;

    public MessagesHub(IMessageService messages, IUserService users, IValidator<PostMessageDto> validator)
    {
        _messages = messages;
        _users = users;
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

    // Feeds the green "Çevrimiçi" dot: connecting/disconnecting refreshes LastSeenAt.
    public override async Task OnConnectedAsync()
    {
        await _users.UpdateLastSeenAsync(CurrentUserId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await _users.UpdateLastSeenAsync(CurrentUserId);
        await base.OnDisconnectedAsync(exception);
    }

    private Guid CurrentUserId => Guid.Parse(Context.UserIdentifier!);
}
