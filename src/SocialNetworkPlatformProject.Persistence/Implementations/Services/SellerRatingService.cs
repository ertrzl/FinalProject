using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.DTOs.Marketplace;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class SellerRatingService : ISellerRatingService
{
    private readonly ISellerRatingRepository _ratings;
    private readonly IListingOfferRepository _offers;
    private readonly IUserRepository _users;
    private readonly INotificationService _notifications;

    public SellerRatingService(
        ISellerRatingRepository ratings,
        IListingOfferRepository offers,
        IUserRepository users,
        INotificationService notifications)
    {
        _ratings = ratings;
        _offers = offers;
        _users = users;
        _notifications = notifications;
    }

    public async Task<GetSellerRatingDto> RateAsync(Guid currentUserId, Guid offerId, PostSellerRatingDto dto)
    {
        var offer = await _offers.GetAll(o => o.Id == offerId, asNoTracking: true, includes: "Listing").FirstOrDefaultAsync()
            ?? throw new NotFoundException("Deal not found.");

        if (offer.BuyerId != currentUserId)
            throw new ForbiddenException("Only the buyer of an accepted deal can rate the seller.");

        if (offer.Status != OfferStatus.Accepted)
            throw new BadRequestException("You can only rate a seller once the deal has been accepted.");

        var comment = string.IsNullOrWhiteSpace(dto.Comment) ? null : dto.Comment.Trim();
        var rating = await _ratings.GetAll(r => r.OfferId == offerId).FirstOrDefaultAsync();
        var isNew = rating == null;

        if (rating == null)
        {
            rating = new SellerRating
            {
                SellerId = offer.SellerId,
                BuyerId = currentUserId,
                OfferId = offerId,
                ListingTitle = offer.Listing?.Title ?? string.Empty,
                Stars = dto.Stars,
                Comment = comment
            };
            await _ratings.AddAsync(rating);
        }
        else
        {
            rating.Stars = dto.Stars;
            rating.Comment = comment;
            rating.UpdatedAt = DateTime.UtcNow;
        }

        try
        {
            await _ratings.SaveChangesAsync();
        }
        catch (DbUpdateException) when (isNew)
        {
            // A double submit lost the race for the one-rating-per-deal unique index.
            throw new ConflictException("This deal was just rated. Refresh and edit your rating instead.");
        }

        // Only the first rating notifies the seller; edits would just be noise.
        if (isNew)
            await _notifications.CreateAsync(offer.SellerId, currentUserId, NotificationType.MarketplaceRatingReceived,
                offerId: offerId, amount: dto.Stars);

        return (await BuildDtosAsync(new[] { rating }))[0];
    }

    public async Task DeleteAsync(Guid currentUserId, Guid offerId)
    {
        var rating = await _ratings.GetAll(r => r.OfferId == offerId).FirstOrDefaultAsync()
            ?? throw new NotFoundException("Rating not found.");

        if (rating.BuyerId != currentUserId)
            throw new ForbiddenException("You can only delete your own rating.");

        _ratings.Delete(rating);
        await _ratings.SaveChangesAsync();
    }

    public async Task<GetSellerRatingSummaryDto> GetSummaryAsync(Guid sellerId)
    {
        var seller = await _users.GetSummaryAsync(sellerId)
            ?? throw new NotFoundException("User not found.");

        // Grouping happens in SQL: five rows at most, however many reviews the seller has.
        var perStar = await _ratings.GetAll(r => r.SellerId == sellerId, asNoTracking: true)
            .GroupBy(r => r.Stars)
            .Select(g => new { Stars = g.Key, Count = g.Count() })
            .ToListAsync();

        var distribution = new int[5];
        foreach (var row in perStar)
            distribution[row.Stars - 1] = row.Count;

        var count = distribution.Sum();
        return new GetSellerRatingSummaryDto
        {
            SellerId = sellerId,
            SellerName = seller.FullName,
            SellerAvatarUrl = seller.AvatarUrl,
            Count = count,
            Average = count == 0 ? null : distribution.Select((n, i) => n * (i + 1)).Sum() / (double)count,
            Distribution = distribution
        };
    }

    public async Task<PagedResult<GetSellerRatingDto>> GetSellerRatingsAsync(Guid sellerId, int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var total = await _ratings.GetAll(r => r.SellerId == sellerId).CountAsync();
        var items = await _ratings.GetAll(
                filter: r => r.SellerId == sellerId,
                orderBy: r => r.CreatedAt,
                isDescending: true,
                asNoTracking: true,
                page: page,
                take: pageSize)
            .ToListAsync();

        return new PagedResult<GetSellerRatingDto>
        {
            Items = await BuildDtosAsync(items),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    // Reviewer names/avatars live on the Identity users: one batched lookup for the whole page.
    private async Task<List<GetSellerRatingDto>> BuildDtosAsync(IEnumerable<SellerRating> ratings)
    {
        var list = ratings.ToList();
        var reviewers = await _users.GetSummariesAsync(list.Select(r => r.BuyerId));

        return list.Select(r =>
        {
            reviewers.TryGetValue(r.BuyerId, out var reviewer);
            return new GetSellerRatingDto
            {
                Id = r.Id,
                OfferId = r.OfferId,
                ReviewerId = r.BuyerId,
                ReviewerName = reviewer?.FullName ?? string.Empty,
                ReviewerAvatarUrl = reviewer?.AvatarUrl,
                Stars = r.Stars,
                Comment = r.Comment,
                ListingTitle = r.ListingTitle,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            };
        }).ToList();
    }
}
