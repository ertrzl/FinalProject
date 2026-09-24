using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.DTOs.Comments;
using SocialNetworkPlatformProject.Application.DTOs.Common;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class CommentService : ICommentService
{
    private static readonly string[] LikeIncludes = { "Likes" };

    private readonly ICommentRepository _comments;
    private readonly ICommentLikeRepository _likes;
    private readonly IPostRepository _posts;
    private readonly IUserRepository _users;
    private readonly IPostAccessService _access;
    private readonly INotificationService _notifications;
    private readonly ILiveUpdateService _live;
    private readonly IMapper _mapper;

    public CommentService(
        ICommentRepository comments,
        ICommentLikeRepository likes,
        IPostRepository posts,
        IUserRepository users,
        IPostAccessService access,
        INotificationService notifications,
        ILiveUpdateService live,
        IMapper mapper)
    {
        _comments = comments;
        _likes = likes;
        _posts = posts;
        _users = users;
        _access = access;
        _notifications = notifications;
        _live = live;
        _mapper = mapper;
    }

    public async Task<List<GetCommentDto>> GetByPostAsync(Guid currentUserId, Guid postId)
    {
        var post = await _posts.GetByIdAsync(postId)
            ?? throw new NotFoundException("Post not found.");

        await _access.EnsureCanViewAsync(post, currentUserId);

        var comments = await _comments.GetAll(
                filter: c => c.PostId == postId,
                orderBy: c => c.CreatedAt,
                asNoTracking: true,
                includes: LikeIncludes)
            .ToListAsync();

        return await BuildDtosAsync(comments, currentUserId);
    }

    public async Task<GetCommentDto> AddAsync(Guid currentUserId, PostCommentDto dto)
    {
        var post = await _posts.GetByIdAsync(dto.PostId)
            ?? throw new NotFoundException("Post not found.");

        await _access.EnsureCanViewAsync(post, currentUserId);

        Comment? parent = null;
        if (dto.ParentCommentId.HasValue)
        {
            parent = await _comments.GetByIdAsync(dto.ParentCommentId.Value)
                ?? throw new NotFoundException("The comment you are replying to no longer exists.");

            if (parent.PostId != dto.PostId)
                throw new BadRequestException("The parent comment belongs to a different post.");
        }

        var comment = new Comment
        {
            PostId = dto.PostId,
            AuthorId = currentUserId,
            Text = dto.Text.Trim(),
            // Threads are one level deep: replying to a reply attaches to the original comment.
            ParentCommentId = parent?.ParentCommentId ?? parent?.Id
        };

        await _comments.AddAsync(comment);
        await _comments.SaveChangesAsync();

        await _notifications.CreateAsync(post.AuthorId, currentUserId, NotificationType.CommentAdded,
            postId: post.Id, commentId: comment.Id);

        if (parent != null && parent.AuthorId != post.AuthorId)
        {
            await _notifications.CreateAsync(parent.AuthorId, currentUserId, NotificationType.CommentAdded,
                postId: post.Id, commentId: comment.Id);
        }

        var result = (await BuildDtosAsync(new[] { comment }, currentUserId))[0];
        await _live.CommentAddedAsync(post.AuthorId, result, await CountForPostAsync(post.Id));
        return result;
    }

    public async Task DeleteAsync(Guid currentUserId, Guid commentId)
    {
        var comment = await _comments.GetByIdAsync(commentId)
            ?? throw new NotFoundException("Comment not found.");

        var post = await _posts.GetByIdAsync(comment.PostId);
        var isPostOwner = post != null && post.AuthorId == currentUserId;

        if (comment.AuthorId != currentUserId && !isPostOwner)
            throw new ForbiddenException("You can only delete your own comments.");

        // The reply self-reference is Restrict (SQL Server forbids a second cascade path), so replies go first.
        var replies = await _comments.GetAll(c => c.ParentCommentId == commentId).ToListAsync();
        foreach (var reply in replies)
            _comments.Delete(reply);

        _comments.Delete(comment);
        await _comments.SaveChangesAsync();

        var removedIds = replies.Select(r => r.Id).Append(comment.Id).ToList();
        await _live.CommentDeletedAsync(post?.AuthorId ?? comment.AuthorId, currentUserId, comment.PostId, removedIds, await CountForPostAsync(comment.PostId));
    }

    public async Task<GetLikeResultDto> ToggleLikeAsync(Guid currentUserId, Guid commentId)
    {
        var comment = await _comments.GetByIdAsync(commentId)
            ?? throw new NotFoundException("Comment not found.");

        var post = await _posts.GetByIdAsync(comment.PostId)
            ?? throw new NotFoundException("Post not found.");

        await _access.EnsureCanViewAsync(post, currentUserId);

        var existing = await _likes.GetAll(l => l.CommentId == commentId && l.UserId == currentUserId).FirstOrDefaultAsync();
        var isLiked = existing == null;

        if (existing != null)
            _likes.Delete(existing);
        else
            await _likes.AddAsync(new CommentLike { CommentId = commentId, UserId = currentUserId });

        await _likes.SaveChangesAsync();

        if (isLiked)
        {
            await _notifications.CreateAsync(comment.AuthorId, currentUserId, NotificationType.CommentLiked,
                postId: comment.PostId, commentId: commentId);
        }

        var count = await _likes.GetAll(l => l.CommentId == commentId).CountAsync();
        await _live.CommentLikeCountChangedAsync(post.AuthorId, currentUserId, comment.PostId, commentId, count);
        return new GetLikeResultDto { IsLiked = isLiked, LikeCount = count };
    }

    private async Task<int> CountForPostAsync(Guid postId)
    {
        return await _comments.GetAll(c => c.PostId == postId).CountAsync();
    }

    private async Task<List<GetCommentDto>> BuildDtosAsync(IEnumerable<Comment> comments, Guid currentUserId)
    {
        var list = comments.ToList();
        var authors = await _users.GetSummariesAsync(list.Select(c => c.AuthorId));

        var dtos = _mapper.Map<List<GetCommentDto>>(list);
        for (var i = 0; i < dtos.Count; i++)
        {
            if (authors.TryGetValue(list[i].AuthorId, out var author))
            {
                dtos[i].AuthorName = author.FullName;
                dtos[i].AuthorAvatarUrl = author.AvatarUrl;
            }

            dtos[i].IsLikedByCurrentUser = list[i].Likes.Any(l => l.UserId == currentUserId);
        }

        return dtos;
    }
}
