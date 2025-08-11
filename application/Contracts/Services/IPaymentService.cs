using domain.Response;
using PayStack.Net;

namespace application.Contracts.Response
{
    public interface IPaymentService
    {
        TransactionVerifyResponse CheckTransactionByRefQuery(string trxReference);

        Task<PaystackRecipientResponse> CreateRecipient(string name, string accountNumber, string bankCode);

        Task<AccountName> GetAccountName(string accNo, string bankCode);

        Task<List<BankInfo>> GetAllBanks();

        Task<PaystackRoot> InitiateTransferHttp(int amount, string transactionCode, string reason);

        TransactionInitializeResponse MakePaystackDeposit(string email, string reference, int amount);
    }
}