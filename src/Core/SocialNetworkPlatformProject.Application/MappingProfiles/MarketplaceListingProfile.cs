using AutoMapper;
using SocialNetworkPlatformProject.Application.DTOs.Marketplace;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.MappingProfiles;

public class MarketplaceListingProfile : Profile
{
    public MarketplaceListingProfile()
    {
        CreateMap<ListingImage, ListingImageDto>();

        CreateMap<MarketplaceListing, GetMarketplaceListingDto>()
            .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.Images.OrderBy(i => i.SortOrder)))
            .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.Condition, opt => opt.MapFrom(src => src.Condition.ToString()))
            .ForMember(dest => dest.IsExpired, opt => opt.Ignore())
            .ForMember(dest => dest.IsFeatured, opt => opt.Ignore())
            .ForMember(dest => dest.CanMessageSeller, opt => opt.Ignore())
            .ForMember(dest => dest.CanRenew, opt => opt.Ignore())
            .ForMember(dest => dest.CanFeature, opt => opt.Ignore())
            .ForMember(dest => dest.NextRenewAt, opt => opt.Ignore())
            .ForMember(dest => dest.NextFeatureAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsSavedByCurrentUser, opt => opt.Ignore())
            .ForMember(dest => dest.MyOpenOfferId, opt => opt.Ignore())
            .ForMember(dest => dest.OpenOfferCount, opt => opt.Ignore())
            .ForMember(dest => dest.SellerRatingAverage, opt => opt.Ignore())
            .ForMember(dest => dest.SellerRatingCount, opt => opt.Ignore())
            .ForMember(dest => dest.SellerName, opt => opt.Ignore())
            .ForMember(dest => dest.SellerAvatarUrl, opt => opt.Ignore());
    }
}
