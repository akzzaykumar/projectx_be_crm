using ActivoosCRM.Application.Features.Authentication.Commands.RegisterUser;
using ActivoosCRM.Domain.Entities;
using AutoMapper;

namespace ActivoosCRM.Application.Features.Authentication.Commands.RegisterUser;

/// <summary>
/// AutoMapper profile for user registration
/// </summary>
public class RegisterUserMappingProfile : Profile
{
    public RegisterUserMappingProfile()
    {
        CreateMap<User, RegisterUserResponse>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role))
            .ForMember(dest => dest.IsEmailVerified, opt => opt.MapFrom(src => src.IsEmailVerified));
    }
}