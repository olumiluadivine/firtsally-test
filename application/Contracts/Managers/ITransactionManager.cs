using application.Commands.Transactions;
using application.Queries.Transactions;

namespace application.Contracts.Managers
{
    public interface ITransactionManager
    {
        Task<ConfirmDepositResult> ConfirmDeposit(string paymentReference, string paystackReference, CancellationToken cancellationToken = default);

        Task<ConfirmWithdrawalResult> ConfirmWithdrawal(string transferReference, string paystackTransferCode, bool isSuccessful, CancellationToken cancellationToken = default);

        Task<DepositResult> DirectDeposit(DepositCommand request, CancellationToken cancellationToken);

        Task<GetBanksResult> GetBanks(GetBanksQuery request, CancellationToken cancellationToken);

        Task<MonthlyStatementDto> GetMonthlyStatement(GetMonthlyStatementQuery request, CancellationToken cancellationToken);

        Task<TransactionHistoryDto> GetTransactionHistory(GetTransactionHistoryQuery request, CancellationToken cancellationToken);

        Task<InitiateDepositResult> HandleDeposit(InitiateDepositCommand request, CancellationToken cancellationToken = default);

        Task<InitiateWithdrawalResult> InitiateExternalWithdrawal(InitiateWithdrawalCommand request, CancellationToken cancellationToken = default);

        Task<ConfirmWithdrawalResult> ReverseWithdrawal(string transferReference, string paystackTransferCode, CancellationToken cancellationToken = default);

        Task<TransferResult> Transfer(TransferCommand request, CancellationToken cancellationToken);

        Task<VerifyAccountResult> VerifyAccount(VerifyAccountQuery request, CancellationToken cancellationToken);

        Task<WithdrawResult> Withdraw(WithdrawCommand request, CancellationToken cancellationToken);
    }
}