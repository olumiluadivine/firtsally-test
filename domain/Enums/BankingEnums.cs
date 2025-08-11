namespace domain.Enums;

public enum AccountType
{
    Savings = 1,
    Current = 2,
    FixedDeposit = 3
}

public enum TransactionType
{
    Deposit = 1,
    Withdrawal = 2,
    Transfer = 3,
    TransferReceived = 4
}

public enum TransactionStatus
{
    Pending = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}