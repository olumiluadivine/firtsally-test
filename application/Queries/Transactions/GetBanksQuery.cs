using application.Contracts.Managers;
using domain.Response;
using MediatR;

namespace application.Queries.Transactions;

public record GetBanksQuery() : IRequest<GetBanksResult>;

public record GetBanksResult(
    IEnumerable<BankInfo> Banks,
    bool IsSuccessful = true,
    string Message = "Banks retrieved successfully"
);

public class GetBanksQueryHandler(ITransactionManager transactionManager) : IRequestHandler<GetBanksQuery, GetBanksResult>
{
    public async Task<GetBanksResult> Handle(GetBanksQuery request, CancellationToken cancellationToken)
    {
        return await transactionManager.GetBanks(request, cancellationToken);
    }
}