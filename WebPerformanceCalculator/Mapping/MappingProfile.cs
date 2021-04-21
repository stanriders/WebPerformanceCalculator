
using AutoMapper;
using WebPerformanceCalculator.DB.Types;
using WebPerformanceCalculator.Models;

namespace WebPerformanceCalculator.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Player, PlayerModel>();

            CreateMap<CalculatedPlayerModel, Player>()
                .ForMember(dest => dest.Id, src => src.MapFrom(x => x.UserID))
                .ForMember(dest => dest.Name, src => src.MapFrom(x => x.Username))
                .ForMember(dest => dest.Country, src => src.MapFrom(x => x.UserCountry))
                .ForMember(dest => dest.LivePp, src => src.MapFrom(x => x.LivePP))
                .ForMember(dest => dest.LocalPp, src => src.MapFrom(x => x.LocalPP))
                .ForMember(dest => dest.PlaycountPp, src => src.MapFrom(x => x.LivePP));
        }
        
    }
}
