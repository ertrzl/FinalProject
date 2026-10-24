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
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.User.RequireUniqueEmail = true;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddErrorDescriber<LocalizedIdentityErrorDescriber>()
            .AddDefaultTokenProviders();

        services.Configure<DataProtectionTokenProviderOptions>(options =>
            options.TokenLifespan = TimeSpan.FromMinutes(configuration.GetValue("PasswordReset:LinkLifetimeMinutes", 60.0)));

        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IPostRepository, PostRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<IPostLikeRepository, PostLikeRepository>();
        services.AddScoped<IPollVoteRepository, PollVoteRepository>();
        services.AddScoped<ICommentLikeRepository, CommentLikeRepository>();
        services.AddScoped<ISavedPostRepository, SavedPostRepository>();
        services.AddScoped<IHashtagRepository, HashtagRepository>();

        services.AddScoped<IFriendRequestRepository, FriendRequestRepository>();
        services.AddScoped<IFriendshipRepository, FriendshipRepository>();
        services.AddScoped<IUserBlockRepository, UserBlockRepository>();

        services.AddScoped<INotificationRepository, NotificationRepository>();

        services.AddScoped<IStoryRepository, StoryRepository>();
        services.AddScoped<IStoryViewRepository, StoryViewRepository>();
        services.AddScoped<IStoryLikeRepository, StoryLikeRepository>();
        services.AddScoped<IStoryTagRepository, StoryTagRepository>();

        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IConversationParticipantRepository, ConversationParticipantRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IMessageReactionRepository, MessageReactionRepository>();

        services.AddScoped<IGroupRepository, GroupRepository>();
        services.AddScoped<IGroupMemberRepository, GroupMemberRepository>();
        services.AddScoped<IGroupJoinRequestRepository, GroupJoinRequestRepository>();
        services.AddScoped<IGroupInviteRepository, GroupInviteRepository>();
        services.AddScoped<IGroupRuleRepository, GroupRuleRepository>();
        services.AddScoped<IGroupPinnedPostRepository, GroupPinnedPostRepository>();

        services.AddScoped<IMarketplaceListingRepository, MarketplaceListingRepository>();
        services.AddScoped<IListingImageRepository, ListingImageRepository>();
        services.AddScoped<ISavedListingRepository, SavedListingRepository>();
        services.AddScoped<ISellerRatingRepository, SellerRatingRepository>();
        services.AddScoped<IListingOfferRepository, ListingOfferRepository>();
        services.AddScoped<IListingOfferRoundRepository, ListingOfferRoundRepository>();
        services.AddScoped<IListingBoostRepository, ListingBoostRepository>();

        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IEventAttendeeRepository, EventAttendeeRepository>();
        services.AddScoped<IEventInviteRepository, EventInviteRepository>();
        services.AddScoped<IEventCommentRepository, EventCommentRepository>();
        services.AddScoped<IEventPhotoRepository, EventPhotoRepository>();

        services.AddScoped<IGameRepository, GameRepository>();

        services.AddScoped<PasswordVerifier>();
        services.AddSingleton<LoginThrottle>();
        services.AddScoped<SessionIssuer>();
        services.AddScoped<ISessionService, SessionService>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPasswordResetService, PasswordResetService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IFriendService, FriendService>();
        services.AddScoped<IBlockService, BlockService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IPostAccessService, PostAccessService>();
        services.AddScoped<IMentionService, MentionService>();
        services.AddScoped<IPostService, PostService>();
        services.AddScoped<IPostDtoBuilder, PostDtoBuilder>();
        services.AddScoped<IPollService, PollService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<IStoryService, StoryService>();
        services.AddScoped<IStoryMaintenanceService, StoryMaintenanceService>();
        services.AddScoped<IDataCleanupService, DataCleanupService>();
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<IChatGroupService, ChatGroupService>();
        services.AddScoped<IGroupService, GroupService>();
        services.AddScoped<IGroupPostService, GroupPostService>();
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
        services.AddScoped<IEventPhotoService, EventPhotoService>();
        services.AddScoped<IEventCheckInService, EventCheckInService>();
        services.AddScoped<ILiveUpdateService, LiveUpdateService>();
        services.AddScoped<IGameService, GameService>();

        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IReportService, ReportService>();

        services.AddScoped<IStoryHighlightRepository, StoryHighlightRepository>();
        services.AddScoped<IStoryHighlightItemRepository, StoryHighlightItemRepository>();
        services.AddScoped<IPostMentionRepository, PostMentionRepository>();
        services.AddScoped<IStoryHighlightService, StoryHighlightService>();
        services.AddScoped<IMutualConnectionService, MutualConnectionService>();
        services.AddScoped<IPostMentionService, PostMentionService>();

        services.AddScoped<IUploadAccessService, UploadAccessService>();

        return services;
    }
}
