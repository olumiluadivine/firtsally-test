namespace domain.Response;

public class WithdrawalCacheData
{
    public Guid AccountId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string BankCode { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string TransferReference { get; set; } = string.Empty;
    public string? PaystackRecipientCode { get; set; }
    public string? PaystackTransferCode { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}