using application.Contracts.Managers;
using domain.Enums;
using MediatR;

namespace application.Queries.Users;

public record GetUserAccountsQuery(Guid UserId) : IRequest<UserAccountsDto>;

public record UserAccountsDto(
    Guid UserId,
    string FullName,
    IEnumerable<UserAccountDto> Accounts
);

public record UserAccountDto(
    Guid Id,
    string AccountNumber,
    AccountType AccountType,
    decimal Balance,
    string Currency,
    bool IsActive,
    DateTime CreatedAt
);

public class GetUserAccountsQueryHandler(IUserManager userManager) : IRequestHandler<GetUserAccountsQuery, UserAccountsDto>
{
    public async Task<UserAccountsDto> Handle(GetUserAccountsQuery request, CancellationToken cancellationToken)
    {
        return await userManager.GetUserAccount(request, cancellationToken);
    }
}