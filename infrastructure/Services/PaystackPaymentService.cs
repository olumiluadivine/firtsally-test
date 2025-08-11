using application.Contracts.Response;
using application.Contracts.Services;
using domain.Response;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PayStack.Net;
using System.Net.Http.Json;

namespace infrastructure.Services;

internal class PaystackPaymentService(
        ILogger<PaystackPaymentService> logger,
        IHttpClientFactory paystackAPI,
        ICacheService cache,
        IConfiguration configuration) : IPaymentService
{
    private readonly HttpClient _paystackAPI = paystackAPI.CreateClient("paystackAPI");
    private readonly PayStackApi _payStackApi = new PayStackApi(configuration["Paystack:SecretKey"]);

    public TransactionVerifyResponse CheckTransactionByRefQuery(string trxReference)
    {
        try
        {
            logger.LogInformation("Verifying Paystack transaction with reference: {TrxReference}", trxReference);

            var request = _payStackApi.Transactions.Verify(trxReference);

            logger.LogInformation("Successfully verified Paystack transaction with reference: {TrxReference}", trxReference);

            return request;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while verifying transaction with reference: {TrxReference}", trxReference);
            throw;
        }
    }

    public async Task<PaystackRecipientResponse> CreateRecipient(string name, string accountNumber, string bankCode)
    {
        logger.LogInformation("Creating Paystack recipient: {Name}, Account: {AccountNumber}, Bank: {BankCode}",
            name, accountNumber, bankCode);

        var data = new
        {
            type = "nuban",
            name = name,
            account_number = accountNumber,
            bank_code = bankCode,
            currency = "NGN"
        };

        try
        {
            var request = await _paystackAPI.PostAsJsonAsync("/transferrecipient", data);
            var response = await request.Content.ReadFromJsonAsync<PaystackRecipientResponse>();

            if (response?.status == true)
            {
                logger.LogInformation("Recipient created successfully: {RecipientCode}", response.data.recipient_code);
            }
            else
            {
                logger.LogWarning("Failed to create recipient: {Message}", response?.message);
            }

            return response ?? new PaystackRecipientResponse { status = false, message = "No response from Paystack" };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating Paystack recipient for {AccountNumber}", accountNumber);
            throw;
        }
    }

    public async Task<AccountName> GetAccountName(string accNo, string bankCode)
    {
        var cacheKey = $"{accNo}-{bankCode}";

        try
        {
            logger.LogInformation("Attempting to retrieve account name for account number {accNo} and bank code {bankCode} from cache: {cacheKey}", accNo, bankCode, cacheKey);

            var accountName = await cache.GetOrSetDataAsync(cacheKey, async () =>
            {
                logger.LogInformation("Account name not found in cache: {cacheKey}", cacheKey);
                logger.LogInformation("Fetching account name from Paystack API...");

                var accountNameResp = _payStackApi.Miscellaneous.ResolveAccountNumber(accNo, bankCode);
                //var accountNameResp = await _paystackApi.GetFromJsonAsync<AccountNameResp>($"/bank/resolve?account_number={accNo}&bank_code={bankCode}");
                if (accountNameResp != null && accountNameResp.Data != null)
                {
                    logger.LogInformation("Account name retrieved successfully from API");
                    return new AccountName
                    {
                        account_name = accountNameResp.Data.AccountName,
                        account_number = accountNameResp.Data.AccountNumber,
                    };
                }
                else
                {
                    logger.LogWarning("Account name not found in API response");
                    return null;
                }
            }, TimeSpan.FromDays(60));

            if (accountName != null)
            {
                logger.LogInformation("Account name retrieved successfully from cache or API: {cacheKey}", cacheKey);
                return accountName;
            }
            else
            {
                logger.LogWarning("Account name retrieval failed for account number {accNo} and bank code {bankCode}", accNo, bankCode);
                return null!;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving account name for account number {accNo} and bank code {bankCode}", accNo, bankCode);
            throw;
        }
    }

    public async Task<List<BankInfo>> GetAllBanks()
    {
        string cacheKey = "nigerianBanks";

        try
        {
            logger.LogInformation("Attempting to retrieve Nigerian banks from cache: {cacheKey}", cacheKey);

            // Use the cache locking mechanism to retrieve or set the data
            var cacheResult = await cache.GetOrSetDataAsync(cacheKey, async () =>
            {
                logger.LogInformation("Nigerian banks not found in cache: {cacheKey}", cacheKey);
                logger.LogInformation("Fetching Nigerian banks from Paystack API...");

                var banks = _payStackApi.Miscellaneous.ListBanks();

                var obj = banks!.Data.Select(bank => new BankInfo
                {
                    Name = bank.Name,
                    Code = bank.Code,
                }).ToList();

                logger.LogInformation("Nigerian banks retrieved successfully from API");
                if (obj != null) return obj;
                return default!;
            }, TimeSpan.FromDays(30));

            if (cacheResult != null)
            {
                logger.LogInformation("Nigerian banks retrieved successfully from cache: {cacheKey}", cacheKey);
            }
            else
            {
                logger.LogWarning("Failed to retrieve Nigerian banks from cache and API");
            }

            return cacheResult!;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving Nigerian banks");
            throw;
        }
    }

    public async Task<PaystackRoot> InitiateTransferHttp(int amount, string transactionCode, string reason)
    {
        logger.LogInformation("Initiating transfer with amount: {Amount}, transactionCode: {TransactionCode}, reason: {Reason}, source: {Source}", amount, transactionCode, reason, "balance");

        object data = new
        {
            source = "balance",
            amount = amount,
            reason = reason,
            recipient = transactionCode,
        };

        try
        {
            logger.LogInformation("Sending POST request to Paystack with data: {Data}", data);

            var request = await _paystackAPI.PostAsJsonAsync("/transfer", data);

            logger.LogInformation("POST request to Paystack succeeded.");
            var response = await request.Content.ReadFromJsonAsync<PaystackRoot>();

            logger.LogInformation("Transfer initiated successfully. Response: {Response}", response);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initiating transfer with transactionCode: {TransactionCode}", transactionCode);
            throw;
        }
    }

    public TransactionInitializeResponse MakePaystackDeposit(string email, string reference, int amount)
    {
        var test = new TransactionInitializeRequest
        {
            Email = email,
            AmountInKobo = amount * 100,
            Currency = "NGN",
            Reference = reference,
            Channels = new[] { "card", "bank", "ussd", "qr", "mobile_money", "bank_transfer", "eft" },
            Bearer = "subaccount",
        };

        try
        {
            logger.LogInformation("Initializing Paystack deposit for reference: {Reference}, email: {Email}, amount: {Amount}", reference, email, amount);

            var request = _payStackApi.Transactions.Initialize(test);

            logger.LogInformation("Successfully initialized Paystack deposit for reference: {Reference}", reference);

            return request;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing Paystack deposit for reference: {Reference}", reference);
            throw;
        }
    }
}