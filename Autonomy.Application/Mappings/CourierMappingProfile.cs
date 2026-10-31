using AutoMapper;
using Autonomy.Application.DTOs.Couriers;
using Autonomy.Domain.Entities;
using Autonomy.Domain.Enums;

namespace Autonomy.Application.Mappings;

public class CourierMappingProfile : Profile
{
    public CourierMappingProfile()
    {
        CreateMap<Courier, CourierDto>();

        CreateMap<CreateCourierDto, Courier>()
            .ForMember(destination => destination.Id, options => options.Ignore())
            .ForMember(destination => destination.CreatedAt, options => options.Ignore())
            .ForMember(destination => destination.UpdatedAt, options => options.Ignore())
            .ForMember(destination => destination.Fleet, options => options.Ignore())
            .ForMember(destination => destination.IsEnabled, options => options.Ignore())
            .ForMember(
                destination => destination.OnlineStatus,
                options => options.MapFrom(_ => CourierOnlineStatus.Offline))
            .ForMember(
                destination => destination.AvailabilityStatus,
                options => options.MapFrom(_ => CourierAvailabilityStatus.Unavailable))
            .ForMember(destination => destination.LastConnectionAt, options => options.Ignore())
            .ForMember(destination => destination.CurrentAssignmentId, options => options.Ignore())
            .ForMember(destination => destination.ActiveDeliveryCount, options => options.Ignore());

        CreateMap<UpdateCourierDto, Courier>()
            .ForMember(destination => destination.Id, options => options.Ignore())
            .ForMember(destination => destination.FleetId, options => options.Ignore())
            .ForMember(destination => destination.UserId, options => options.Ignore())
            .ForMember(destination => destination.LastConnectionAt, options => options.Ignore())
            .ForMember(destination => destination.CurrentAssignmentId, options => options.Ignore())
            .ForMember(destination => destination.ActiveDeliveryCount, options => options.Ignore())
            .ForMember(destination => destination.CreatedAt, options => options.Ignore())
            .ForMember(destination => destination.UpdatedAt, options => options.Ignore())
            .ForMember(destination => destination.Fleet, options => options.Ignore());
    }
}
