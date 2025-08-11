using application.Contracts.Managers;
using MediatR;

namespace application.Queries.Transactions;

public record GetMonthlyStatementQuery(
    Guid AccountId,
    int Year,
    int Month
) : IRequest<MonthlyStatementDto>;

public record MonthlyStatementDto(
    Guid AccountId,
    string AccountNumber,
    int Year,
    int Month,
    decimal OpeningBalance,
    decimal ClosingBalance,
    decimal TotalDeposits,
    decimal TotalWithdrawals,
    int TransactionCount,
    IEnumerable<TransactionDto> Transactions
);

public class GetMonthlyStatementQueryHandler(ITransactionManager transactionManager) : IRequestHandler<GetMonthlyStatementQuery, MonthlyStatementDto>
{
    public async Task<MonthlyStatementDto> Handle(GetMonthlyStatementQuery request, CancellationToken cancellationToken)
    {
        return await transactionManager.GetMonthlyStatement(request, cancellationToken);
    }
}