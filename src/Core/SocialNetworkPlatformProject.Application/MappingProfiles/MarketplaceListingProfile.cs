using AutoMapper;
using SocialNetworkPlatformProject.Application.DTOs.Marketplace;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.MappingProfiles;

public class MarketplaceListingProfile : Profile
{
    public MarketplaceListingProfile()
    {
        CreateMap<MarketplaceListing, GetMarketplaceListingDto>()
            .ForMember(dest => dest.SellerName, opt => opt.Ignore())
            .ForMember(dest => dest.SellerAvatarUrl, opt => opt.Ignore());
    }
}
