using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Persistence.Contexts;
using SocialNetworkPlatformProject.Persistence.Identity;
using SocialNetworkPlatformProject.Persistence.Implementations.Repositories;
using SocialNetworkPlatformProject.Persistence.Implementations.Services;

namespace SocialNetworkPlatformProject.Persistence;

public static class ServiceRegistration
{
    public static IServiceCollection AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                // Relaxed for local dev/demo purposes — tighten before a real deployment.
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        // F3–F5
        services.AddScoped<IPostRepository, PostRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<IPostLikeRepository, PostLikeRepository>();
        services.AddScoped<ICommentLikeRepository, CommentLikeRepository>();
        services.AddScoped<ISavedPostRepository, SavedPostRepository>();
        services.AddScoped<IHashtagRepository, HashtagRepository>();

        // F2
        services.AddScoped<IFriendRequestRepository, FriendRequestRepository>();
        services.AddScoped<IFriendshipRepository, FriendshipRepository>();

        // F8
        services.AddScoped<INotificationRepository, NotificationRepository>();

        // Stories
        services.AddScoped<IStoryRepository, StoryRepository>();

        // Messages
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IConversationParticipantRepository, ConversationParticipantRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();

        // Groups
        services.AddScoped<IGroupRepository, GroupRepository>();
        services.AddScoped<IGroupMemberRepository, GroupMemberRepository>();
        services.AddScoped<IGroupJoinRequestRepository, GroupJoinRequestRepository>();
        services.AddScoped<IGroupInviteRepository, GroupInviteRepository>();

        // Marketplace
        services.AddScoped<IMarketplaceListingRepository, MarketplaceListingRepository>();
        services.AddScoped<IListingImageRepository, ListingImageRepository>();
        services.AddScoped<ISavedListingRepository, SavedListingRepository>();
        services.AddScoped<ISellerRatingRepository, SellerRatingRepository>();
        services.AddScoped<IListingOfferRepository, ListingOfferRepository>();
        services.AddScoped<IListingOfferRoundRepository, ListingOfferRoundRepository>();

        // Events
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IEventAttendeeRepository, EventAttendeeRepository>();
        services.AddScoped<IEventInviteRepository, EventInviteRepository>();
        services.AddScoped<IEventCommentRepository, EventCommentRepository>();

        // Users (Identity-backed, so not a generic IRepository<T>)
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        // Services (business logic)
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IFriendService, FriendService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IPostAccessService, PostAccessService>();
        services.AddScoped<IPostService, PostService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<IStoryService, StoryService>();
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<IGroupService, GroupService>();
        services.AddScoped<IMarketplaceService, MarketplaceService>();
        services.AddScoped<IOfferService, OfferService>();
        services.AddScoped<ISellerRatingService, SellerRatingService>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IEventAccessService, EventAccessService>();
        services.AddScoped<IEventInviteService, EventInviteService>();
        services.AddScoped<IEventCommentService, EventCommentService>();
        services.AddScoped<ILiveUpdateService, LiveUpdateService>();

        return services;
    }
}
