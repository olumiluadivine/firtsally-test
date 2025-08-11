using application.Contracts.Managers;
using MediatR;

namespace application.Queries.Transactions;

public record VerifyAccountQuery(
    string AccountNumber,
    string BankCode
) : IRequest<VerifyAccountResult>;

public record VerifyAccountResult(
    string AccountNumber,
    string AccountName,
    string BankCode,
    bool IsSuccessful,
    string Message
);

public class VerifyAccountQueryHandler(ITransactionManager transactionManager) : IRequestHandler<VerifyAccountQuery, VerifyAccountResult>
{
    public async Task<VerifyAccountResult> Handle(VerifyAccountQuery request, CancellationToken cancellationToken)
    {
        return await transactionManager.VerifyAccount(request, cancellationToken);
    }
}