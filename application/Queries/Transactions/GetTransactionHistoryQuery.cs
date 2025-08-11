using application.Contracts.Managers;
using domain.Enums;
using MediatR;

namespace application.Queries.Transactions;

public record GetTransactionHistoryQuery(
    Guid AccountId,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int PageNumber = 1,
    int PageSize = 50
) : IRequest<TransactionHistoryDto>;

public record TransactionHistoryDto(
    Guid AccountId,
    IEnumerable<TransactionDto> Transactions,
    int TotalCount,
    int PageNumber,
    int PageSize
);

public record TransactionDto(
    Guid Id,
    TransactionType Type,
    decimal Amount,
    string Currency,
    string Description,
    DateTime CreatedAt,
    TransactionStatus Status,
    string Reference,
    Guid? RelatedAccountId
);

public class GetTransactionHistoryQueryHandler(ITransactionManager transactionManager) : IRequestHandler<GetTransactionHistoryQuery, TransactionHistoryDto>
{
    public async Task<TransactionHistoryDto> Handle(GetTransactionHistoryQuery request, CancellationToken cancellationToken)
    {
        return await transactionManager.GetTransactionHistory(request, cancellationToken);
    }
}