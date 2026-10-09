using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Persistence.Contexts;
using SocialNetworkPlatformProject.Persistence.Identity;
using SocialNetworkPlatformProject.Persistence.Implementations;
using SocialNetworkPlatformProject.Persistence.Implementations.Repositories;
using SocialNetworkPlatformProject.Persistence.Implementations.Services;

namespace SocialNetworkPlatformProject.Persistence;

public static class ServiceRegistration
{
    public static IServiceCollection AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                // Relaxed for local dev/demo purposes — tighten before a real deployment.
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.User.RequireUniqueEmail = true;

                // Password guessing: after 5 wrong passwords the account is locked for 15 minutes.
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        // The code in a password reset link works for an hour (Identity's own default is a day).
        services.Configure<DataProtectionTokenProviderOptions>(options =>
            options.TokenLifespan = TimeSpan.FromMinutes(configuration.GetValue("PasswordReset:LinkLifetimeMinutes", 60.0)));

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
        services.AddScoped<IStoryViewRepository, StoryViewRepository>();

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

        // Login sessions and password checks (Identity-backed)
        services.AddScoped<PasswordVerifier>();
        services.AddScoped<SessionIssuer>();

        // Users (Identity-backed, so not a generic IRepository<T>)
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        // Services (business logic)
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPasswordResetService, PasswordResetService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IFriendService, FriendService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IPostAccessService, PostAccessService>();
        services.AddScoped<IPostService, PostService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<IStoryService, StoryService>();
        services.AddScoped<IStoryMaintenanceService, StoryMaintenanceService>();
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<IGroupService, GroupService>();
        services.AddScoped<IMarketplaceService, MarketplaceService>();
        services.AddScoped<IOfferService, OfferService>();
        services.AddScoped<ISellerRatingService, SellerRatingService>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IEventQueryService, EventQueryService>();
        services.AddScoped<IEventCalendarService, EventCalendarService>();
        services.AddScoped<IEventDtoBuilder, EventDtoBuilder>();
        services.AddScoped<IEventAccessService, EventAccessService>();
        services.AddScoped<IEventNotifier, EventNotifier>();
        services.AddScoped<IEventMaintenanceService, EventMaintenanceService>();
        services.AddScoped<IEventInviteService, EventInviteService>();
        services.AddScoped<IEventCommentService, EventCommentService>();
        services.AddScoped<ILiveUpdateService, LiveUpdateService>();

        return services;
    }
}
