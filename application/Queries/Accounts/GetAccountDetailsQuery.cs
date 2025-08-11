using application.Contracts.Managers;
using domain.Enums;
using MediatR;

namespace application.Queries.Accounts;

public record GetAccountDetailsQuery(Guid AccountId) : IRequest<AccountDetailsDto>;

public record AccountDetailsDto(
    Guid Id,
    string AccountNumber,
    decimal Balance,
    string Currency,
    AccountType AccountType,
    DateTime CreatedAt,
    bool IsActive
);

public class GetAccountDetailsQueryHandler(IAccountManager accountManager) : IRequestHandler<GetAccountDetailsQuery, AccountDetailsDto>
{
    public async Task<AccountDetailsDto> Handle(GetAccountDetailsQuery request, CancellationToken cancellationToken)
    {
        return await accountManager.GetAccount(request, cancellationToken);
    }
}