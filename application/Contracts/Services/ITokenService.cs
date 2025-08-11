using domain.Entities;

namespace application.Contracts.Services
{
    public interface ITokenService
    {
        string CreateToken(User user);
    }
}