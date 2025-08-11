using application.Commands.Accounts;
using application.Queries.Accounts;

namespace application.Contracts.Managers
{
    public interface IAccountManager
    {
        Task<ChangePINResult> ChangePin(ChangePINCommand request, CancellationToken cancellationToken);

        Task<CreateAccountResult> CreateAccount(CreateAccountCommand request, CancellationToken cancellationToken);

        Task<AccountDetailsDto> GetAccount(GetAccountDetailsQuery request, CancellationToken cancellationToken);
    }
}