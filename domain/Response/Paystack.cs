namespace domain.Response;

public class BankInfo
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public class AccountName
{
    public string account_number { get; set; } = string.Empty;
    public string account_name { get; set; } = string.Empty;
    public int? bank_id { get; set; }
}

public class PaystackRoot
{
    public bool status { get; set; }
    public string message { get; set; } = string.Empty;
    public TransferData data { get; set; } = new();
}

public class TransferData
{
    public int integration { get; set; }
    public string domain { get; set; } = string.Empty;
    public int amount { get; set; }
    public string currency { get; set; } = string.Empty;
    public string source { get; set; } = string.Empty;
    public string reason { get; set; } = string.Empty;
    public int recipient { get; set; }
    public string status { get; set; } = string.Empty;
    public string transfer_code { get; set; } = string.Empty;
    public int id { get; set; }
    public DateTime createdAt { get; set; }
    public DateTime updatedAt { get; set; }
}

public class RecipientData
{
    public string recipient_code { get; set; } = string.Empty;
    public string name { get; set; } = string.Empty;
    public string type { get; set; } = string.Empty;
    public RecipientDetails details { get; set; } = new();
    public bool active { get; set; }
    public DateTime created_at { get; set; }
    public DateTime updated_at { get; set; }
}

public class RecipientDetails
{
    public string account_number { get; set; } = string.Empty;
    public string account_name { get; set; } = string.Empty;
    public string bank_code { get; set; } = string.Empty;
    public string bank_name { get; set; } = string.Empty;
}

public class PaystackRecipientResponse
{
    public bool status { get; set; }
    public string message { get; set; } = string.Empty;
    public RecipientData data { get; set; } = new();
}