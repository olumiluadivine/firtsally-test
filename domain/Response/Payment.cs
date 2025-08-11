namespace domain.Services;

public record ProcessDepositRequest(
    string AccountNumber,
    decimal Amount,
    string Currency,
    string Description,
    string CustomerEmail
);

public record ProcessWithdrawalRequest(
    string AccountNumber,
    decimal Amount,
    string Currency,
    string Description,
    string CustomerEmail,
    string BankCode,
    string AccountName
);

public record PaymentResult(
    bool IsSuccessful,
    string PaymentReference,
    string Message,
    decimal Amount,
    string Currency,
    DateTime ProcessedAt
);

public enum PaymentStatus
{
    Pending,
    Successful,
    Failed,
    Cancelled
}