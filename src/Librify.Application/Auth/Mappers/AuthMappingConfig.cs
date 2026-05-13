using Librify.Application.Auth.Dtos;
using Librify.Domain.Entities;
using Mapster;

namespace Librify.Application.Auth.Mappers;

public class AuthMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<User, RegisterResponse>()
            .Map(dest => dest.UserId, src => src.Id);
    }
}
