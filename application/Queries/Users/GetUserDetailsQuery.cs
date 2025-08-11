using application.Contracts.Managers;
using MediatR;

namespace application.Queries.Users;

public record GetUserDetailsQuery(Guid UserId) : IRequest<UserDetailsDto>;

public record UserDetailsDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    DateTime CreatedAt,
    bool IsActive,
    bool IsEmailVerified,
    DateTime? LastLoginAt
);

public class GetUserDetailsQueryHandler(IUserManager userManager) : IRequestHandler<GetUserDetailsQuery, UserDetailsDto>
{
    public async Task<UserDetailsDto> Handle(GetUserDetailsQuery request, CancellationToken cancellationToken)
    {
        return await userManager.GetUserDetails(request, cancellationToken);
    }
}