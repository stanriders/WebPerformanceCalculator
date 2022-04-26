
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

            CreateMap<ApiPlayerModel, Player>()
                .ForMember(dest => dest.Id, src => src.MapFrom(x => x.Id))
                .ForMember(dest => dest.Name, src => src.MapFrom(x => x.Username))
                .ForMember(dest => dest.Country, src => src.MapFrom(x => x.Country))
                .ForMember(dest => dest.LivePp, src => src.MapFrom(x => x.Pp));

            CreateMap<ApiScore, Score>()
                .ForMember(dest => dest.Id, src => src.MapFrom(x => x.Id))
                .ForMember(dest => dest.Accuracy, src => src.MapFrom(x => x.Accuracy))
                .ForMember(dest => dest.Combo, src => src.MapFrom(x => x.Combo))
                .ForMember(dest => dest.LivePp, src => src.MapFrom(x => x.Pp))
                .ForMember(dest => dest.Mods, src => src.MapFrom(x => string.Join(", ", x.Mods)))
                .ForMember(dest => dest.Misses, src => src.MapFrom(x => x.Statistics.CountMiss))
                .ForMember(dest => dest.PlayerId, src => src.MapFrom(x => x.UserId))
                .ForMember(dest => dest.MapId, src => src.MapFrom(x => x.BeatmapShort.Id));
        }
    }
}
