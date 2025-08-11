using application.Commands.Users;
using application.Queries.Users;

namespace application.Contracts.Managers
{
    public interface IUserManager
    {
        Task<UserAccountsDto> GetUserAccount(GetUserAccountsQuery request, CancellationToken cancellationToken);

        Task<UserDetailsDto> GetUserDetails(GetUserDetailsQuery request, CancellationToken cancellationToken);

        Task<LoginResult> Login(LoginCommand request, CancellationToken cancellationToken);

        Task<RegisterUserResult> Register(RegisterUserCommand request, CancellationToken cancellationToken);
    }
}